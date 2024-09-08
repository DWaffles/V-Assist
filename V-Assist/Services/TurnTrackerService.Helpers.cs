using DSharpPlus.Entities;
using VAssist.Common;
using VAssist.Trackers;

namespace VAssist.Services
{
    internal partial class TurnTrackerService
    {
        internal static List<DiscordActionRowComponent> GetActionRowComponents(int num_teams)
        {
            // 1st : Turn Buttons // Reaction Buttons // Toggle Reaction, Cycle Reaction Amount
            // 2nd : Join Team Dropdown
            // 3rd : Controller Action Button Row
            // 4th : Director Action Button Row // Add NPC // Remove // Kill // Rename?
            // 5th : Selected Action Interaction Drop Down
            // 5th : DM Selected Character? // <-

            // Next Turn
            // Mark incapabe = strike through

            var options = new List<DiscordSelectComponentOption>();
            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++)
            {
                options.Add(new(label: $"Join {DefaultTeamNames[i]}", value: $"tts_dropdown_{i}"));
            }
            options.Add(new(label: $"Leave team", value: $"tts_dropdown_leave"));
            var dropdown = new DiscordSelectComponent(customId: "tts_dropdown", placeholder: "Players, join a team. Director, add characters to a team.", options, disabled: false, minOptions: 0, maxOptions: 1);

            return
            [
                new DiscordActionRowComponent(TurnTrackerRowOne),
                new DiscordActionRowComponent([dropdown])
            ];
        }
        /// <summary>
        /// Gets a model representation of a turn tracker <see cref="DiscordEmbed"/>.
        /// </summary>
        /// <param name="embed"></param>
        /// <returns>A <see cref="TurnTrackerModel"/>, which represents the changable data of a turn tracker.</returns>
        internal static TurnTrackerModel ParseTurnTracker(DiscordEmbed embed)
        {
            var trackerVersion = embed.Footer.Text;
            // Check Version is current

            // Create a variable that will only hold the fields referring to teams of characters
            var team_fields = embed.Fields?.ToList() ?? [];

            // From the builder, get the fields that we need to extract the value for.
            var director_field = team_fields.First(f => f.Name.Equals("Director"));
            var controller_field = team_fields.First(f => f.Name.Equals("Current Controller"));
            var rotation_field = team_fields.First(f => f.Name.StartsWith("Rotation History"));

            // Remove fields from the team_fields variable that do not correlate to teams
            team_fields.Remove(director_field);
            team_fields.Remove(controller_field);
            team_fields.Remove(rotation_field);
            team_fields.Remove(team_fields.Last(f => f.Name.Equals("Director Controlled Character"))); // Current Action is after user named fields, so use Last
            team_fields.Remove(team_fields.Last(f => f.Name.Equals("Key"))); // Key is after user named fields, so use Last

            return new()
            {
                DirectorId = Util.ParseUlong(director_field.Value), // Parse required director Id
                ControllerId = Util.ParseUlongOrNull(controller_field.Value), // Parse optional controller Id
                TurnNumber = Util.ParseInt(rotation_field.Name), // Parse required turn number
                RotationHistory = rotation_field.Value, // Parse rotation history
                TrackerVersion = trackerVersion, // Parse tracker version
                Teams = ParseTurnTrackerTeams(team_fields) // Parse teams and characters from all of the team fields
            };
        }
        /// <summary>
        /// Returns <see cref="List{T}"/> of <see cref="TurnTrackerModel"/> from a given <see cref="List{T}"/> of fields representing a team from a turn tracker.
        /// </summary>
        /// <param name="teamFields">A <see cref="List{DiscordEmbedField}"/> of the team fields from the turn tracker to pass.</param>
        /// <returns>A <see cref="List{TurnTrackerModel}"/> constructed from the team fields.</returns>
        internal static List<TurnTrackerTeamModel> ParseTurnTrackerTeams(List<DiscordEmbedField> teamFields)
        {
            var list = new List<TurnTrackerTeamModel>(); // create a variable to hold all of the teams in the builder
            foreach (var field in teamFields)
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
        /// Parse a <see cref="TurnTrackerCharacterModel"/> from a <see cref="string"/> line under a team field in a turn tracker builder.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to parse.</param>
        /// <returns>The parsed <see cref="TurnTrackerCharacterModel"/>.</returns>
        internal static TurnTrackerCharacterModel ParseTurnTrackerCharacter(string str)
        {
            // Check to see if the string contains a mention
            // If it does, it is a player character, otherwise, an NPC of director control
            bool mention = Util.TryParseMention(str, out ulong id);
            var reactions = Util.MatchNumbers(str[str.LastIndexOf('[')..str.LastIndexOf(']')]); // parse all the numbers in the reaction section
            return new()
            {
                CharacterName = mention
                    ? null
                    : str[str.IndexOf("**")..str.LastIndexOf("**")], // Character Name
                PlayerID = mention
                    ? id
                    : null, // player ID
                ReactionsAvailable = (int)reactions.First(),
                ReactionsMax = (int)reactions.Last(),
                TurnAvailable = str.Contains(Green) || str.Contains(Blue),
            };
        }
        /// <summary>
        /// Remove a <see cref="DiscordUser"/> from all the teams in a Turn Tracker.
        /// </summary>
        /// <param name="user">The <see cref="DiscordUser"/> to remove.</param>
        /// <param name="turnTracker">The Turn Tracker to modify.</param>
        internal static void RemovePlayerCharacterFromTeams(DiscordUser user, TurnTrackerModel turnTracker)
        {
            for (int i = 0; i < turnTracker.Teams.Count; i++) // Remove the user from all other teams they may be apart of
            {
                var userCharacters = turnTracker.Teams[i].Characters.Where(ch => ch.PlayerID != null && ch.PlayerID.Equals(user.Id)).ToList();
                foreach (var character in userCharacters) // remove any found characters of this user.
                {
                    turnTracker.Teams[i].Characters.Remove(character);
                }
            }
        }
        /// <summary>
        /// Adds a <see cref="DiscordUser"/> to a team in a Turn Tracker.
        /// </summary>
        /// <param name="user">The <see cref="DiscordUser"/> to add,</param>
        /// <param name="turnTracker">The Turn Tracker to modify.</param>
        /// <param name="optionId">The Id of the interacting <see cref="DiscordSelectComponentOption"/> dropdown option.</param>
        internal static void AddPlayerCharacterToTeam(DiscordUser user, TurnTrackerModel turnTracker, string optionId)
        {
            int teamPos = Util.ParseInt(optionId); // get the position/index of the team the user wishes to choice
            var team = turnTracker.Teams[teamPos]; // grab the team

            team.Characters.Add(new() // add the user with base values
            {
                CharacterName = null,
                PlayerID = user.Id,
                ReactionsAvailable = 1,
                ReactionsMax = 1,
                TurnAvailable = true
            });
        }
        /// <summary>
        /// Updates the various changable fields in TurnTracker.
        /// </summary>
        /// <param name="builder">The <see cref="DiscordEmbedBuilder"/> representation of the Turn Tracker to update.</param>
        /// <param name="turnTracker">The Turn Tracker to update from.</param>
        internal static void UpdateTurnTracker(DiscordEmbedBuilder builder, TurnTrackerModel turnTracker)
        {
            foreach (var team in turnTracker.Teams) // Update the teams and characters with the updated values
            {
                var field = builder.Fields.Single(field => field.Name.Equals(team.TeamName));
                field.Value = team.ToString();
            }
        }
    }
}