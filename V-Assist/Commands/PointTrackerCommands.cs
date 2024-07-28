using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System.ComponentModel;

namespace VAssist.Commands
{
    internal class PointTrackerCommands
    {
        [Command("narrative-point-tracker"), Description("Create a new narrative point tracker")]
        public static ValueTask NarrativePointTracker(SlashCommandContext ctx,
            [Description("Number of narrative points to start the session with"), MinMaxValue(minValue: 1)] int narrative_points,
            [Description("Name or number of the session. Optional")] string? session_name = null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: session_name == null ? "Narrative Point Tracker" : $"Session {session_name}", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle($"Current Narrative Points: {narrative_points}")
                .AddField($"Initial Points: {narrative_points}", $"By {ctx.User.Mention} @ <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>")
                .WithFooter(text: ctx.Client.CurrentUser.Username)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"))
                .Build();

            var builder = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(style: DiscordButtonStyle.Primary, customId: "npt_spend", label: "Spend Point", emoji: new DiscordComponentEmoji("🔽")),
                    new DiscordButtonComponent(style: DiscordButtonStyle.Danger, customId: "npt_add", label: "Add Point [GM]", emoji: new DiscordComponentEmoji("🔼")),

                })
                .AddComponents(new DiscordComponent[]
                {
                    new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "npt_for", label: "Add Reason", emoji: new DiscordComponentEmoji("🗒️"), disabled: true),
                    new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "npt_end", label: "End Session [GM]", emoji: new DiscordComponentEmoji("⏹️")),
                });

            return ctx.RespondAsync(builder);
        }
    }
}