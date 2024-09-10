using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using VAssist.Common;
using VAssist.Models;

namespace VAssist.Services
{
    internal class NarrativePointTrackerService
    {
        internal static DiscordComponent[] PointTrackButtonRowOne { get; } = [
            new DiscordButtonComponent(style: DiscordButtonStyle.Primary, customId: "npt_spend", label: "Spend Point", emoji: new DiscordComponentEmoji("🔽")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Danger, customId: "npt_add", label: "Add Point [GM]", emoji: new DiscordComponentEmoji("🔼"))];
        internal static DiscordComponent[] PointTrackButtonRowTwo { get; } = [new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "npt_for", label: "Add Reason", emoji: new DiscordComponentEmoji("🗒️"), disabled: true),
            new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "npt_end", label: "End Session [GM]", emoji: new DiscordComponentEmoji("⏹️"))];
        internal static DiscordButtonComponent Button_NPT_BecomeDirector { get; } = new(style: DiscordButtonStyle.Secondary, customId: "npt_bgm", label: "Become GM");
        internal static DiscordButtonComponent Button_NPT_ResignDirector { get; } = new(style: DiscordButtonStyle.Secondary, customId: "npt_rgm", label: "Resign GM");

        // UNDO BUTTON
        // NEW SESSION BUTTON
        internal DiscordEmbed GetNewEmbed(SlashCommandContext ctx, int party_points, int total_points, string? session_name = null, DiscordUser? director = null)
        {
            return new DiscordEmbedBuilder()
                .WithAuthor(name: session_name == null ? "Narrative Point Tracker" : $"{session_name}", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle($"Party Narrative Points: {party_points}")
                .AddField($"Initial Points: {party_points}", $"By {ctx.User.Mention} @ <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>", inline: true)
                .AddField($"Director Points: {total_points - party_points}", director is null ? "Not Assigned" : director.Mention, inline: true)
                .WithFooter(text: ctx.Client.CurrentUser.Username) // current point changes 0/23
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"))
                .Build();
        }
        internal List<DiscordActionRowComponent> GetComponents(DiscordUser? director)
        {
            var directorRow = new List<DiscordButtonComponent>()
            {
                director is null ? Button_NPT_BecomeDirector : Button_NPT_ResignDirector
            };
            return
            [
                new(PointTrackButtonRowOne),
                new(PointTrackButtonRowTwo),
                new(directorRow),
            ];
        }
        internal bool CheckEmbedMaxFields(DiscordEmbed embed)
        {
            return embed.Fields?.Count >= 25;
        }
        internal bool CheckDirectorAction(DiscordMessage message, DiscordUser user)
        {
            var embed = message.Embeds[0];
            var dField = embed.Fields.Single(f => f.Name.Contains("Director"));
            var dIdUtil = Util.ParseUlongOrNull(dField.Value);

            return dIdUtil == null || dIdUtil == user.Id;
        }
        internal NarrativePointTrackerModel ParseNarrativePointTrackerInteraction(IEnumerable<DiscordActionRowComponent> components, DiscordEmbed embed) // cut down, condense
        {
            var initialField = embed.Fields.Single(f => f.Name.Contains("Initial"));
            var directorField = embed.Fields.Single(f => f.Name.Contains("Director"));

            ulong? director = Util.ParseUlongOrNull(directorField.Value); // director ID
            int partyNarrativePoints = (int)Util.ParseUlongOrNull(embed.Title); // party points // does not handle negative points
            int directoryNarrativePoints = (int)Util.ParseUlongOrNull(directorField.Name); // director points // does not handle negative points

            var fields = embed.Fields.ToList(); // fields
            fields.RemoveRange(0, 2);

            return new NarrativePointTrackerModel()
            {
                DirectorId = director,
                PartyNarrativePoints = partyNarrativePoints,
                DirectorNarrativePoints = directoryNarrativePoints,
                InitialPoints = (initialField.Name, initialField.Value),
                ButtonRows = components.Select(row => row.Components.ToList()).ToList() // buttons
            };
        }
        internal void ParsePointChangeField(string fieldValue, out ulong unixTime, out string? reason)
        {
            var matches = Util.MatchUlongs(fieldValue);
            unixTime = matches.Count >= 2 ? matches[1] : 0;
            reason = null;

            if (fieldValue.Contains("Reason: "))
            {
                int index = fieldValue.IndexOf("Reason: ");
                reason = fieldValue[(index + "Reason: ".Length)..];
            }
        }

        internal DiscordEmbedBuilder ModifyTrackerPoints(DiscordEmbedBuilder embed, DiscordUser user, int old_points, int new_points, int director_points)
        {
            embed.Fields[1].Name = $"Director Points: {director_points}";
            return embed.WithTitle($"Party Narrative Points: {new_points}")
                .AddField($"{old_points} -> {new_points}", $"By {user.Mention} @ <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>", inline: false);
        }
        internal DiscordWebhookBuilder HandlePointSpend(DiscordMessage message, DiscordUser user)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);
            wrapper.ButtonRows.ForEach(row => row.ForEach(button => button = ((DiscordButtonComponent)button).Enable()));

            int old_points = wrapper.PartyNarrativePoints;
            int new_points = old_points - 1;
            int director_points = wrapper.DirectorNarrativePoints + 1;
            embed = ModifyTrackerPoints(embed, user, old_points, new_points, director_points);

            if (new_points <= 0)
            {
                foreach (var row in wrapper.ButtonRows)
                {
                    var test = row.SingleOrDefault(button => button.CustomId == "npt_spend");
                    if (test != null)
                        test = ((DiscordButtonComponent)test).Disable();
                }
            }

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
        internal DiscordWebhookBuilder HandlePointAdd(DiscordMessage message, DiscordUser user)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);
            wrapper.ButtonRows.ForEach(row => row.ForEach(button => button = ((DiscordButtonComponent)button).Enable()));

            int old_points = wrapper.PartyNarrativePoints;
            int new_points = old_points + 1;
            int director_points = wrapper.DirectorNarrativePoints - 1;
            embed = ModifyTrackerPoints(embed, user, old_points, new_points, director_points);

            if ((wrapper.DirectorNarrativePoints - 1) <= 0)
            {
                foreach (var row in wrapper.ButtonRows)
                {
                    var buttonAdd = row.SingleOrDefault(button => button.CustomId == "npt_add");
                    if (buttonAdd != null)
                        buttonAdd = ((DiscordButtonComponent)buttonAdd).Disable();
                }
            }

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
        internal DiscordWebhookBuilder HandleTrackerEnd(DiscordMessage message)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);
            wrapper.ButtonRows.ForEach(row => row.ForEach(button => button = ((DiscordButtonComponent)button).Disable()));

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
        internal DiscordWebhookBuilder HandleDirectorAssign(DiscordMessage message, DiscordUser user) // become director
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);

            var field = embed.Fields.Single(f => f.Name.Contains("Director"));
            field.Value = user.Mention;

            foreach (var row in wrapper.ButtonRows)
            {
                var index = row.FindIndex(0, match => match.CustomId == "npt_bgm");
                if (index != -1)
                {
                    row[index] = Button_NPT_ResignDirector;
                }
            }

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
        internal DiscordWebhookBuilder HandleDirectorResign(DiscordMessage message)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed); // resign director

            var field = embed.Fields.Single(f => f.Name.Contains("Director"));
            field.Value = "Not Assigned";

            foreach (var row in wrapper.ButtonRows)
            {
                var index = row.FindIndex(0, match => match.CustomId == "npt_rgm");
                if (index != -1)
                {
                    row[index] = Button_NPT_BecomeDirector;
                }
            }

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
        internal DiscordInteractionResponseBuilder HandleAddReason(DiscordMessage message, DiscordUser user)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);

            var userChanges = embed.Fields.Where(f => f.Value.Contains(user.Mention)).Select(f => (f.Name, f.Value)).ToList();
            if (userChanges.Count > 5)
            {
                userChanges = userChanges.GetRange(userChanges.Count - 5, 5);
            }
            else if (userChanges.Count == 0)
            {
                return new DiscordInteractionResponseBuilder();
            }

            var builder = new DiscordInteractionResponseBuilder()
                .WithCustomId("modal_npt")
                .WithTitle("Add Reason");
            foreach (var (Name, Value) in userChanges)
            {
                ParsePointChangeField(Value, out ulong unixTime, out string? reason);
                builder.AddComponents(new DiscordTextInputComponent(label: $"{Name} by You", customId: unixTime.ToString(), value: reason, required: false));
            }
            return builder;
        }
        internal DiscordWebhookBuilder HandleTrackerModal(DiscordMessage message, DiscordUser user, IReadOnlyDictionary<string, string> modalValues)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var wrapper = ParseNarrativePointTrackerInteraction(message.Components, embed);

            foreach (var field in embed.Fields)
            {
                ParsePointChangeField(field.Value, out ulong unixTime, out string? reason);

                if (unixTime != 0 && modalValues.ContainsKey(unixTime.ToString()))
                {
                    string? testVal = modalValues[unixTime.ToString()];
                    if (string.IsNullOrEmpty(testVal))
                    {
                        field.Value = $"By {user.Mention} @ <t:{unixTime}:t>";
                    }
                    else
                    {
                        field.Value = $"By {user.Mention} @ <t:{unixTime}:t>. Reason: {testVal}";
                    }
                }
            }

            return new DiscordWebhookBuilder()
                .AddEmbed(embed)
                .AddComponents(wrapper.ButtonRows.Select(row => new DiscordActionRowComponent(row)));
        }
    }
}