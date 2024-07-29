using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
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
            int partyNarrativePoints = (int) Util.ParseUlong(embed.Title); // party points
            int directoryNarrativePoints = (int) Util.ParseUlong(directorField.Name); // director points

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

        // Handle Add Points

        // Handle Sub Points
    }
}
