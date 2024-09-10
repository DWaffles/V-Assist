using DSharpPlus.Entities;
using VAssist.Common;
using VAssist.Trackers;

namespace VAssist.Services
{
    internal partial class TurnTrackerService
    {
        /// <summary>
        /// Gets a <see cref="DiscordEmbed"/> representing a new turn tracker.
        /// </summary>
        /// <param name="ctx">The base context for slash command contexts.</param>
        /// <param name="num_teams">The number of teams to include in the new turn tracker.</param>
        /// <param name="director">The <see cref="DiscordUser"/> who will serve as the director for the turn tracker.</param>
        /// <returns>A <see cref="DiscordEmbed"/> representing a new turn tracker.</returns>
        private static DiscordEmbed GetNewEmbed(DiscordUser currentUser, int num_teams, DiscordUser director)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: Resources.TurnTracker.AuthorName, iconUrl: currentUser.AvatarUrl)
                .AddField(Resources.TurnTracker.ControllerFieldName, Resources.TurnTracker.ControllerFieldValueDefault, inline: true)
                .AddField(Resources.TurnTracker.DirectorFieldName, director.Mention, inline: true)
                .AddField(Resources.TurnTracker.RotationFieldNamePrefix + " (Turn 1)", Resources.TurnTracker.RotationFieldValueDefault, inline: false)
                .WithFooter(text: currentUser.Username + " • " + Resources.TurnTracker.TurnTrackerCurrentVersion)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"));

            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++) // Add the number of specified teams to the turn tracker
            {
                embed.AddField(DefaultTeamNames[i], Resources.TurnTracker.TeamFieldValueDefault, inline: true);
            }

            embed.AddField(Resources.TurnTracker.KeyFieldName, $"{Green} = {Resources.TurnTracker.GreenDescription}" +
                                                                $"\n{Blue} = {Resources.TurnTracker.BlueDescription}" +
                                                                $"\n{Orange} = {Resources.TurnTracker.OrangeDescription}" +
                                                                $"\n{Red} = {Resources.TurnTracker.RedDescription}", inline: false);
            embed.AddField(Resources.TurnTracker.DirectorCharactersFieldName, Resources.TurnTracker.DirectorCharactersFieldValueDefault);
            return embed.Build();
        }
        private static List<DiscordActionRowComponent> GetNewComponents(int num_teams)
        {
            return
            [
                new DiscordActionRowComponent(TurnTrackerRowOne),
                new DiscordActionRowComponent(GetTurnTrackerTeamDropDown(num_teams)),
            ];
        }
        private static DiscordWebhookBuilder UpdateTurnTracker(DiscordEmbedBuilder builder, TurnTrackerModel turnTracker)
        {
            UpdateTurnTrackerModel(builder, turnTracker);
            return new DiscordWebhookBuilder()
               .AddEmbed(builder)
               .AddComponents(UpdateComponents(turnTracker));
        }
        /// <summary>
        /// Updates the various changable fields in TurnTracker.
        /// </summary>
        /// <param name="builder">The <see cref="DiscordEmbedBuilder"/> representation of the Turn Tracker to update.</param>
        /// <param name="turnTracker">The Turn Tracker to update from.</param>
        private static void UpdateTurnTrackerModel(DiscordEmbedBuilder builder, TurnTrackerModel turnTracker)
        {
            // Update the teams and characters with the updated values
            foreach (var team in turnTracker.Teams)
            {
                var field_team = builder.Fields.Single(field => field.Name.Equals(team.TeamName));
                field_team.Value = team.ToString();
            }

            // Update selected characters
            var selected = turnTracker.Teams.SelectMany(t => t.Characters).Where(c => c.SelectedByDirector);
            var field_character = builder.Fields.Last(f => f.Name.StartsWith(Resources.TurnTracker.DirectorCharactersFieldName)); // Director Character is after user named fields, so use Last
            if (selected.Any())
            {
                field_character.Value = string.Join('\n', selected.Select(c => c.ToString()));
            }
            else
            {
                field_character.Value = Resources.TurnTracker.DirectorCharactersFieldValueDefault;
            }
        }
        private static List<DiscordActionRowComponent> UpdateComponents(TurnTrackerModel turnTracker)
        {
            // 3rd : Controller Action Button Row
            // 4th : Director Action Button Row // Add NPC // Remove // Kill // Rename?
            // 5th : Selected Action Interaction Drop Down
            // Next Turn

            var components = new List<DiscordActionRowComponent>()
            {
                new(TurnTrackerRowOne),
                new(GetTurnTrackerTeamDropDown(turnTracker))
            };

            var chSelect = GetTurnTrackerCharacterSelect(turnTracker);
            if(chSelect != null)
            {
                components.Add(new(chSelect));
            }

            return components;

        }
        private static DiscordSelectComponent[] GetTurnTrackerTeamDropDown(int num_teams)
        {
            var options = new List<DiscordSelectComponentOption>();
            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++)
            {
                options.Add(new(label: $"Join {DefaultTeamNames[i]}", value: $"tts_dropdown_{i}"));
            }
            options.Add(new(label: Resources.TurnTracker.LeaveTeamOptionLabel, value: $"tts_dropdown_leave"));
            return [new(customId: "tts_dropdown_team_join", placeholder: Resources.TurnTracker.TeamSelectionPlaceholder, options, disabled: false, minOptions: 0, maxOptions: 1)];
        }
        private static DiscordSelectComponent[] GetTurnTrackerTeamDropDown(TurnTrackerModel turnTracker)
        {
            var options = new List<DiscordSelectComponentOption>();
            for (int i = 0; i < turnTracker.Teams.Count; i++)
            {
                options.Add(new(label: $"Join {turnTracker.Teams[i].TeamName}", value: $"tts_dropdown_{i}"));
            }
            options.Add(new(label: Resources.TurnTracker.LeaveTeamOptionLabel, value: $"tts_dropdown_leave"));
            return [new(customId: "tts_dropdown_team_join", placeholder: Resources.TurnTracker.TeamSelectionPlaceholder, options, disabled: false, minOptions: 0, maxOptions: 1)];
        }
        private static DiscordSelectComponent[]? GetTurnTrackerCharacterSelect(TurnTrackerModel turnTracker)
        {
            var options = new List<DiscordSelectComponentOption>();
            for(int tIndex = 0; tIndex < turnTracker.Teams.Count; tIndex++)
            {
                var team = turnTracker.Teams[tIndex];
                for (int cIndex = 0; cIndex < team.Characters.Count; cIndex++)
                {
                    var cModel = team.Characters[cIndex];
                    if(cModel.PlayerID == null) // Player characters are not human readable in select menu currently
                    {
                        options.Add(new(label: $"Select {cModel.Mention() ?? cModel.CharacterName} from {team.TeamName}",
                                        value: $"tts_dropdown_{tIndex}_{cIndex}",
                                        isDefault: cModel.SelectedByDirector));
                    }
                }
            }

            if (options.Count != 0)
                return [new(customId: "tts_dropdown_character_select", placeholder: Resources.TurnTracker.CharacterSelectionPlaceholder, options, disabled: false, minOptions: 0, maxOptions: Math.Min(options.Count, 5))];
            else
                return null;
        }
        /// <summary>
        /// Remove a <see cref="DiscordUser"/> from all the teams in a Turn Tracker.
        /// </summary>
        /// <param name="user">The <see cref="DiscordUser"/> to remove.</param>
        /// <param name="turnTracker">The Turn Tracker to modify.</param>
        private static void RemovePlayerCharacterFromTeams(DiscordUser user, TurnTrackerModel turnTracker)
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
        private static void AddPlayerCharacterToTeam(DiscordUser user, TurnTrackerModel turnTracker, string optionId)
        {
            int teamPos = Util.ParseInt(optionId); // get the position/index of the team the user wishes to choice
            var team = turnTracker.Teams[teamPos]; // grab the team

            team.Characters.Add(new() // add the user with base values
            {
                CharacterName = null,
                PlayerID = user.Id,
                ReactionsAvailable = 1,
                ReactionsMax = 1,
                TurnAvailable = true,
                SelectedByDirector = false,
            });
        }
    }
}