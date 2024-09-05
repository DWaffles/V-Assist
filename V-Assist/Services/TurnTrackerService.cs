using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VAssist.Services
{
    internal class TurnTrackerService
    {
        internal static List<string> TeamNames = ["Team A", "Team B", "Team C"];
        internal DiscordEmbed GetNewEmbed(SlashCommandContext ctx, int num_teams, DiscordUser? director = null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: "Turn Tracker Tracker", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Current Controller", "Not Assigned", inline: true)
                .AddField("Director", (director?.Mention ?? "Not Assigned"), inline: true)
                .AddField("Rotation History (Turn 1)", "New turn", inline:false)
                .WithFooter(text: ctx.Client.CurrentUser.Username)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"));

            for (int i = 0; i < num_teams && i < TeamNames.Count; i++)
            {
                embed.AddField(TeamNames[i], "Empty", inline: true);
            }

            embed.AddField("Key", ":green_square: = Turn, reaction available" +
                                "\n:orange_square: = Turn unavailable, reaction available." +
                                "\n:red_square: = Turn, reaction unavailable", inline: false);
            embed.AddField("Current Action", "N/A"); // Will represent 
            return embed.Build();
        }
        internal List<DiscordActionRowComponent> GetActionRowComponents(int num_teams)
        {
            // 1st : Turn Buttons (?) // Reaction Buttons // OR Button: Join Team A, B, C, etc, Toggle Reaction, Cycle Reaction Amount
            // 2nd : Join Team Dropdown
            // 3rd : Controller Action Button Row
            // 4th : Director Action Button Row // Add NPC // Remove // Kill // Rename?
            // 5th : Selected Action Interaction Drop Down


            var options = new List<DiscordSelectComponentOption>();
            
            for (int i = 0; i < num_teams && i < TeamNames.Count; i++)
            {
                options.Add(new (label: $"Join {TeamNames[i]}", value: $"tts_dropdown_{i}"));
            }

            var dropdown = new DiscordSelectComponent(customId: "dropdown", placeholder: "Join a team.", options, disabled: false, minOptions: 0, maxOptions: 1);

            return
            [
                new DiscordActionRowComponent([dropdown])
            ];
        }
        internal void ParseTurnTracker()
        {

        }

    }
}
