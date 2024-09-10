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

        [Command("turn-tracker-simple"), Description("Create a new simple turn tracker for a maximum of 25 characters across 5 teams.")]
        public ValueTask TurnTrackerSimple(SlashCommandContext ctx,
            // Support a max of 5 teams due to modals only supporting up to 5 text boxes.
            [Description("The number of teams to include in the turn tracker. Optional, defaults to two, [0,5]."), MinMaxValue(minValue: 1, maxValue: 5)]
                int num_teams = 2,
            [Description("The discord user to be the director for this session. Optional, defaults to executing user.")]
                DiscordUser? director = null)
        {
            director ??= ctx.User;
            return ctx.RespondAsync(TurnTrackerService.GetNewTurnTracker(ctx.Client.CurrentUser, director, num_teams));
        }
    }
}
