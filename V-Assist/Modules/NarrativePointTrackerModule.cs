using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using System.ComponentModel;
using VAssist.Services;

namespace VAssist.Commands
{
    internal class NarrativePointTrackerCommands(NarrativePointTrackerService pointTrackerService)
    {
        internal NarrativePointTrackerService PointTrackerService { get; set; } = pointTrackerService;

        [Command("narrative-point-tracker"), Description("Create a new narrative point tracker. Maximum of 23 narrative point changes.")]
        public ValueTask NarrativePointTracker(SlashCommandContext ctx,
            [Description("The number of narrative points available to the party.")] int party_points,
            [Description("The total number of narrative points in the session.")] int total_points,
            [Description("Name or number of the session. Optional, not prefixed by anything.")] string? session_name = null,
            [Description("The discord user to be the director for this session. Optional.")] DiscordUser? director = null)
        {
            if (party_points > total_points)
            {
                return ctx.RespondAsync("Party narrative points must be less than or equal to the total narrative points", ephemeral: true);
            }
            else if (party_points < 1)
            {
                return ctx.RespondAsync("Party narrative points must be greater than 1.", ephemeral: true);
            }

            var embed = PointTrackerService.GetNewEmbed(ctx: ctx, party_points: party_points, total_points: total_points, session_name: session_name, director: director);
            var components = PointTrackerService.GetComponents(director);
            var builder = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(components);

            return ctx.RespondAsync(builder);
        }

        public ValueTask NarrativePointTrackerOverride(TextCommandContext ctx, DiscordUser director)
        {
            throw new NotImplementedException();
        }
        public ValueTask NarrativePointTrackerOverride(TextCommandContext ctx, string session_name)
        {
            throw new NotImplementedException();
        }
    }
}