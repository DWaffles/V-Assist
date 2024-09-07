using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VAssist.Common;
using VAssist.Trackers;

namespace VAssist.Services
{
    internal class TurnTrackerService
    {
        /// <summary>
        /// The emoji <see cref="string"/> representing that a <see cref="TurnTrackerCharacterModel"/> has their turn and reaction available.
        /// </summary>
        internal static string Green { get; } = ":green_square:";
        /// <summary>
        /// The emoji <see cref="string"/> representing that a <see cref="TurnTrackerCharacterModel"/> has their turn available, and reaction unavailable.
        /// </summary>
        internal static string Blue { get; } = ":blue_square:";
        /// <summary>
        /// The emoji <see cref="string"/> representing that a <see cref="TurnTrackerCharacterModel"/> has their turn unavailable, and reaction available.
        /// </summary>
        internal static string Orange { get; } = ":orange_square:";
        /// <summary>
        /// The emoji <see cref="string"/> representing that a <see cref="TurnTrackerCharacterModel"/> has their turn and reaction unavailable.
        /// </summary>
        internal static string Red { get; } = ":red_square:";
        /// <summary>
        /// List of default team names to use for a new turn tracker.
        /// </summary>
        internal static List<string> DefaultTeamNames { get; } = ["Team A", "Team B", "Team C", "Team D", "Team E", "Team F"];
        /// <summary>
        /// Gets a <see cref="DiscordEmbed"/> representing a new turn tracker.
        /// </summary>
        /// <param name="ctx">The base context for slash command contexts.</param>
        /// <param name="num_teams">The number of teams to include in the new turn tracker.</param>
        /// <param name="director">The <see cref="DiscordUser"/> who will serve as the director for the turn tracker.</param>
        /// <returns>A <see cref="DiscordEmbed"/> representing a new turn tracker.</returns>
        internal DiscordEmbed GetNewEmbed(SlashCommandContext ctx, int num_teams, DiscordUser? director = null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: "Turn Tracker Tracker", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Current Controller", "Not Assigned", inline: true)
                .AddField("Director", (director?.Mention ?? "Not Assigned"), inline: true)
                .AddField("Rotation History (Turn 1)", "New turn", inline:false)
                .WithFooter(text: ctx.Client.CurrentUser.Username + " • TTS v1.0")
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"));

            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++) // Add the number of specified teams to the turn tracker
            {
                embed.AddField(DefaultTeamNames[i], "Empty", inline: true);
            }

            embed.AddField("Key", $"{Green} = Turn, reaction available." +
                                $"\n{Blue} = Turn available, reaction unavailable." +
                                $"\n{Orange} = Turn unavailable, reaction available." +
                                $"\n{Red} = Turn, reaction unavailable.", inline: false);
            embed.AddField("Current Action", "N/A"); // TBD
            return embed.Build();
        }
        internal List<DiscordActionRowComponent> GetActionRowComponents(int num_teams)
        {
            // 1st : Turn Buttons (?) // Reaction Buttons // OR Button: Join Team A, B, C, etc, Toggle Reaction, Cycle Reaction Amount
            // 2nd : Join Team Dropdown
            // 3rd : Controller Action Button Row
            // 4th : Director Action Button Row // Add NPC // Remove // Kill // Rename?
            // 5th : Selected Action Interaction Drop Down

            // Mark incapabe = strike through

            var options = new List<DiscordSelectComponentOption>();
            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++)
            {
                options.Add(new (label: $"Join {DefaultTeamNames[i]}", value: $"tts_dropdown_{i}"));
            }
            options.Add(new(label: $"Leave team", value: $"tts_dropdown_leave"));
            var dropdown = new DiscordSelectComponent(customId: "tts_dropdown", placeholder: "Players, join a team.", options, disabled: false, minOptions: 0, maxOptions: 1);

            return
            [
                new DiscordActionRowComponent([dropdown])
            ];
        }
        /// <summary>
        /// Gets a model representation of a turn tracker <see cref="DiscordEmbed"/>.
        /// </summary>
        /// <param name="embed"></param>
        /// <returns>A <see cref="TurnTrackerModel"/>, which represents the changable data of a turn tracker.</returns>
        internal TurnTrackerModel ParseTurnTracker(DiscordEmbed embed)
        {
            // Create a variable that will only hold the fields referring to teams of characters
            var team_fields = embed.Fields?.ToList() ?? [];

            // From the embed, get the fields that we need to extract the value for.
            var director_field = team_fields.First(f => f.Name.Equals("Director"));
            var controller_field = team_fields.First(f => f.Name.Equals("Current Controller"));
            var rotation_field = team_fields.First(f => f.Name.StartsWith("Rotation History"));

            // Remove fields from the team_fields variable that do not correlate to teams
            team_fields.Remove(director_field);
            team_fields.Remove(controller_field);
            team_fields.Remove(rotation_field);
            team_fields.Remove(team_fields.Last(f => f.Name.Equals("Current Action"))); // Current Action is after user named fields, so use Last
            team_fields.Remove(team_fields.Last(f => f.Name.Equals("Key"))); // Key is after user named fields, so use Last

            return new()
            {
                DirectorId = Util.ParseUlong(director_field.Value), // Parse required director Id
                ControllerId = Util.ParseUlongOrNull(controller_field.Value), // Parse optional controller Id
                TurnNumber = Util.ParseInt(rotation_field.Name), // Parse required turn number
                RotationHistory = rotation_field.Value, // Parse rotation history
                TrackerVersion = embed.Footer.Text, // Parse tracker version
                Teams = ParseTurnTrackerTeams(team_fields) // Parse teams and characters from all of the team fields
            };
        }
        /// <summary>
        /// Returns <see cref="List{T}"/> of <see cref="TurnTrackerModel"/> from a given <see cref="List{T}"/> of fields representing a team from a turn tracker.
        /// </summary>
        /// <param name="teamFields">A <see cref="List{DiscordEmbedField}"/> of the team fields from the turn tracker to pass.</param>
        /// <returns>A <see cref="List{TurnTrackerModel}"/> constructed from the team fields.</returns>
        internal List<TurnTrackerTeamModel> ParseTurnTrackerTeams(List<DiscordEmbedField> teamFields)
        {
            var list = new List<TurnTrackerTeamModel>(); // create a variable to hold all of the teams in the embed
            foreach(var field in teamFields)
            {
                var characters = field.Value.Equals("Empty") // check to see if the team has any characters
                    ? [] // if there are no characters
                    : field.Value.Split('\n').Select(str => ParseTurnTrackerCharacter(str)).ToList(); // otherwise, parse each line into a character model

                list.Add(new() // create a Turn Tracker Team and add the parsed characters
                {
                    TeamName = field.Name,
                    Characters = characters,
                });
            }
            return list;
        }
        /// <summary>
        /// Parse a <see cref="TurnTrackerCharacterModel"/> from a <see cref="string"/> line under a team field in a turn tracker embed.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to parse.</param>
        /// <returns>The parsed <see cref="TurnTrackerCharacterModel"/>.</returns>
        internal TurnTrackerCharacterModel ParseTurnTrackerCharacter(string str)
        {
            // Check to see if the string contains a mention
            // If it does, it is a player character, otherwise, an NPC of director control
            bool mention = Util.TryParseMention(str, out ulong id); 
            var reactions = Util.MatchNumbers(str[str.LastIndexOf('[')..str.LastIndexOf(']')]); // parse all the numbers in the reaction section
            return new()
            {
                CharacterName = mention ? null : str[str.IndexOf("**")..str.LastIndexOf("**")], // Character Name
                PlayerID = mention ? id : null, // player ID
                ReactionsAvailable = (int) reactions.First(),
                ReactionsTotal = (int) reactions.Last(),
                TurnAvailable = str.Contains(Green) || str.Contains(Blue),
            };
        }
        /// <summary>
        /// Function to handle changing the team of a player character for a Turn Tracker.
        /// </summary>
        /// <param name="message">The original <see cref="DiscordMessage"/> of the Turn Tracker <see cref="DiscordEmbed"/>.</param>
        /// <param name="user">The interacting <see cref="DiscordUser"/> to change teams for.</param>
        /// <param name="optionId">The selected option Id the interacting <see cref="DiscordUser"/> chose.</param>
        /// <returns>A <see cref="DiscordWebhookBuilder"/> of the updated Turn Tracker and <see cref="DiscordComponent"/>s.</returns>
        internal DiscordWebhookBuilder HandleTeamChange(DiscordMessage message, DiscordUser user, string optionId)
        {
            var embed = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an embed builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            for(int i = 0; i < turnTracker.Teams.Count; i++) // Remove the user from all other teams they may be apart of
            {
                var userCharacters = turnTracker.Teams[i].Characters.Where(ch => ch.PlayerID != null && ch.PlayerID.Equals(user.Id)).ToList();
                foreach (var character in userCharacters) // remove any found characters of this user.
                {
                    turnTracker.Teams[i].Characters.Remove(character); 
                }
            }

            if(!optionId.Equals("tts_dropdown_leave")) // The user has chosen to sign up for a specific team
            {
                int teamPos = Util.ParseInt(optionId); // get the position/index of the team the user wishes to choice
                var team = turnTracker.Teams[teamPos]; // grab the team

                team.Characters.Add(new() // add the user with base values
                {
                    CharacterName = null,
                    PlayerID = user.Id,
                    ReactionsAvailable = 1,
                    ReactionsTotal = 1,
                    TurnAvailable = true
                });
            }

            foreach(var team in turnTracker.Teams) // Update the embed with the updated values
            {
                var field = embed.Fields.Single(field => field.Name.Equals(team.TeamName));
                field.Value = team.ToString();
            }

            return new DiscordWebhookBuilder()
               .AddEmbed(embed)
               .AddComponents(message.Components);
        }
    }
}