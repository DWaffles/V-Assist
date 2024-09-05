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

        [Command("turn-tracker-simple"), Description("Create a new simple turn tracker for a maximum of 25 characters.")]
        public ValueTask TurnTrackerSimple(SlashCommandContext ctx,
            [Description("The number of teams to include in the turn tracker. Optional, defaults to two, [0,1]."), MinMaxValue(minValue: 1, maxValue: 2)]
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

        /*public ValueTask TurnTrackerSimpleOld(SlashCommandContext ctx,
            [Description("The name for the first group of characters.")] string first_group_name,
            [Description("The name for the second group of characters. Optional.")] string? second_group_name = null,
            [Description("The discord user to be the director for this session. Optional, defaults to executing user.")] DiscordUser? director = null)
        {
            director ??= ctx.User;

            var embed = TurnTrackerService.GetNewEmbed(ctx, [first_group_name, second_group_name], director);
            var components = TurnTrackerService.GetActionRowComponents([first_group_name, second_group_name]);
            var builder = new DiscordMessageBuilder()
                .AddEmbed(embed)
                .AddComponents(components);

            return ctx.RespondAsync(builder);
        }*/

        // Turn Tracker Advanced (tta)
    }
}
