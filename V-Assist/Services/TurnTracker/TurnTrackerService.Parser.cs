using DSharpPlus.Entities;
using VAssist.Common;
using VAssist.Trackers;

namespace VAssist.Services
{
    internal partial class TurnTrackerService
    {
        /// <summary>
        /// Gets a model representation of a turn tracker <see cref="DiscordEmbed"/>.
        /// </summary>
        /// <param name="embed"></param>
        /// <returns>A <see cref="TurnTrackerModel"/>, which represents the changable data of a turn tracker.</returns>
        private static TurnTrackerModel ParseTurnTracker(DiscordEmbed embed)
        {
            var trackerVersion = embed.Footer.Text;
            // Check Version is current

            // Create a variable that will only hold the fields referring to teams of characters
            var team_fields = embed.Fields?.ToList() ?? [];

            // From the builder, get the fields that we need to extract the value for.
            var director_field = team_fields.First(f => f.Name.Equals(Resources.TurnTracker.DirectorFieldName));
            var controller_field = team_fields.First(f => f.Name.Equals(Resources.TurnTracker.ControllerFieldName));
            var rotation_field = team_fields.First(f => f.Name.StartsWith(Resources.TurnTracker.RotationFieldNamePrefix));
            var characters_field = team_fields.Last(f => f.Name.StartsWith(Resources.TurnTracker.DirectorCharactersFieldName)); // Director Character is after user named fields, so use Last

            // Remove fields from the team_fields variable that do not correlate to teams
            team_fields.Remove(director_field);
            team_fields.Remove(controller_field);
            team_fields.Remove(rotation_field);
            team_fields.Remove(characters_field);
            team_fields.Remove(team_fields.Last(f => f.Name.Equals(Resources.TurnTracker.KeyFieldName))); // Key is after user named fields, so use Last

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
        private static List<TurnTrackerTeamModel> ParseTurnTrackerTeams(List<DiscordEmbedField> teamFields)
        {
            var list = new List<TurnTrackerTeamModel>(); // create a variable to hold all of the teams in the builder
            foreach (var field in teamFields)
            {
                var characters = field.Value.Equals(Resources.TurnTracker.TeamFieldValueDefault) // check to see if the team has any characters
                    ? [] // if there are no characters
                    : field.Value.Split('\n').Select(str => ParseTurnTrackerCharacter(str)).ToList(); // otherwise, parse each line into a ch model

                list.Add(new() // create a Turn Tracker Team and add the parsed characters
                {
                    TeamName = field.Name,
                    Characters = characters,
                });
            }
            return list;
        }
        /// <summary>
        /// Parse a <see cref="TurnTrackerCharacterModel"/> from a <see cref="string"/> line under a team field_team in a turn tracker builder.
        /// </summary>
        /// <param name="str">The <see cref="string"/> to parse.</param>
        /// <returns>The parsed <see cref="TurnTrackerCharacterModel"/>.</returns>
        private static TurnTrackerCharacterModel ParseTurnTrackerCharacter(string str)
        {
            // Check to see if the string contains a mention
            // If it does, it is a player ch, otherwise, an NPC of director control
            bool mention = Util.TryParseMention(str, out ulong id);
            var reactions = Util.MatchNumbers(str[str.LastIndexOf('[')..str.LastIndexOf(']')]); // parse all the numbers in the reaction section
            return new()
            {
                CharacterName = mention
                    ? null
                    : str[(str.IndexOf(" ") + 1)..(str.LastIndexOf("[") - 1)], // Character Name
                PlayerID = mention
                    ? id
                    : null, // player ID
                ReactionsAvailable = (int)reactions.First(),
                ReactionsMax = (int)reactions.Last(),
                TurnAvailable = str.Contains(Green) || str.Contains(Blue),
                SelectedByDirector = false,
            };
        }
    }
}