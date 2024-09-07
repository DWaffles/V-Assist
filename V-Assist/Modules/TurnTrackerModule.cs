using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System.ComponentModel;
using VAssist.Services;

namespace VAssist.Modules
{
    internal class TurnTrackerModule(TurnTrackerService turnTrackerService)
    {
        internal TurnTrackerService TurnTrackerService { get; set; } = turnTrackerService;

        [Command("turn-tracker-simple"), Description("Create a new simple turn tracker for a maximum of 25 characters across 6 teams.")]
        public ValueTask TurnTrackerSimple(SlashCommandContext ctx,
            [Description("The number of teams to include in the turn tracker. Optional, defaults to two, [0,6]."), MinMaxValue(minValue: 1, maxValue: 6)]
                int num_teams = 2,
            [Description("The discord user to be the director for this session. Optional, defaults to executing user.")]
                DiscordUser? director = null)
        {
            director ??= ctx.User;

            var embed = TurnTrackerService.GetNewEmbed(ctx, num_teams, director);
            var components = TurnTrackerService.GetActionRowComponents(num_teams);
            var builder = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(components);

            return ctx.RespondAsync(builder);
        }
    }
}
