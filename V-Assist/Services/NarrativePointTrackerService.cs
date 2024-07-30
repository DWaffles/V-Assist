using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Newtonsoft.Json.Linq;
using VAssist.Commands;
using VAssist.Common;
using VAssist.Models;

namespace VAssist.Services
{
    internal class NarrativePointTrackerService
    {
        internal DiscordEmbed GenerateNewEmbed(SlashCommandContext ctx, int party_points, int total_points, string? session_name = null, DiscordUser? director = null)
        {
            return new DiscordEmbedBuilder()
                .WithAuthor(name: session_name == null ? "Narrative Point Tracker" : $"Session {session_name}", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle($"Party Narrative Points: {party_points}")
                .AddField($"Initial Points: {party_points}", $"By {ctx.User.Mention} @ <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>", inline: true)
                .AddField($"Director Points: {total_points - party_points}", director is null ? "Not Assigned" : director.Mention, inline: true)
                .WithFooter(text: ctx.Client.CurrentUser.Username)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"))
                .Build();
        }
        internal NarrativePointTrackerModel ParseNarrativePointTrackerInteraction(IEnumerable<DiscordActionRowComponent> components, DiscordEmbed embed) // cut down, condense
        {
            var initialField = embed.Fields.Single(f => f.Name.Contains("Initial"));
            var directorField = embed.Fields.Single(f => f.Name.Contains("Director"));

            ulong? director = Util.ParseUlong(directorField.Value); // director ID
            int partyNarrativePoints = (int)Util.ParseUlong(embed.Title); // party points // does not handle negative points
            int directoryNarrativePoints = (int)Util.ParseUlong(directorField.Name); // director points // does not handle negative points

            var fields = embed.Fields.ToList(); // fields
            fields.RemoveRange(0, 2);

            return new NarrativePointTrackerModel()
            {
                DirectorId = director,
                PartyNarrativePoints = partyNarrativePoints,
                DirectorNarrativePoints = directoryNarrativePoints,
                InitialPoints = (initialField.Name, initialField.Value), 
                PointChanges = fields.Select(f => (f.Name, f.Value)).ToList(),
                ButtonRows = components.Select(row => row.Components.ToList()).ToList() // buttons
            };
        }
        internal bool EmbedMaxFields(DiscordEmbed embed)
        {
            return embed.Fields?.Count >= 25;
        }
        internal bool AllowDirectorAction(DiscordMessage message, DiscordUser user)
        {
            var embed = message.Embeds[0];
            var dField = embed.Fields.Single(f => f.Name.Contains("Director"));
            var dIdUtil = Util.ParseUlong(dField.Value);

            return dIdUtil == null || dIdUtil == user.Id;
        }
        internal DiscordEmbedBuilder ModifyTrackerEmbed(DiscordEmbedBuilder embed, DiscordUser user, int old_points, int new_points, int director_points)
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
            int new_points = old_points-1;
            int director_points = wrapper.DirectorNarrativePoints + 1;
            embed = ModifyTrackerEmbed(embed, user, old_points, new_points, director_points);

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
            embed = ModifyTrackerEmbed(embed, user, old_points, new_points, director_points);

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
                    row[index] = NarrativePointTrackerCommands.Button_NPT_ResignDirector;
                }
            }

            Console.WriteLine($"1found {wrapper.ButtonRows[2][0].CustomId}");
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
                    row[index] = NarrativePointTrackerCommands.Button_NPT_BecomeDirector;
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

            var userChanges = wrapper.PointChanges.Where(f => f.Value.Contains(user.Mention)).ToList();
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
        internal void ParsePointChangeField(string fieldValue, out ulong unixTime, out string? reason)
        {
            var matches = Util.MatchNumbers(fieldValue);
            unixTime = matches[1];
            reason = null;

            if(fieldValue.Contains("Reason: "))
            {
                int index = fieldValue.IndexOf("Reason: ");
                reason = fieldValue.Substring(index + "Reason: ".Length);
            }
        }
    }
}