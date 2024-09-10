using DSharpPlus.Entities;
using VAssist.Common;
using VAssist.Trackers;

namespace VAssist.Services
{
    internal partial class TurnTrackerService
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
        /// Specific letters and symbols that are not to be allowed in the names of non-player characters. Normally Discord formatting characters that might disrupt the formatting of the Turn Tracker.
        /// </summary>
        private static string[] ProhibitedLetters { get; } = ["*", "_", "`", "~"];
        /// <summary>
        /// List of default team names to use for a new turn tracker.
        /// </summary>
        private static List<string> DefaultTeamNames { get; } = ["Team A", "Team B", "Team C", "Team D", "Team E", "Team F"];
        private static DiscordComponent[] TurnTrackerRowOne { get; } = [
            new DiscordButtonComponent(style: DiscordButtonStyle.Success, customId: "tts_button_turn", label: Resources.TurnTracker.ButtonToggleTurnLabel, emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Primary, customId: "tts_button_reaction_cycle", label: Resources.TurnTracker.ButtonReactionCycleLabel, emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "tts_button_reaction_max", label: Resources.TurnTracker.ButtonReactionMaxLabel, emoji: new DiscordComponentEmoji("🔃")),
        ];
        internal DiscordMessageBuilder GetNewTurnTracker(DiscordUser currentUser, DiscordUser director, int num_teams)
        {
            return new DiscordMessageBuilder()
                .AddEmbed(GetNewEmbed(currentUser, num_teams, director))
                .AddComponents(GetNewComponents(num_teams));
        }
        /// <summary>
        /// Checks to see if a <see cref="DiscordUser"/> is a part of any team on the Turn Tracker associated with the <see cref="DiscordMessage"/>.
        /// </summary>
        /// <param name="message">The Turn Tracker to perform the check on.</param>
        /// <param name="user">The <see cref="DiscordUser"/> to perform the check on.</param>
        /// <returns>True if found, false otherwise.</returns>
        internal bool UserHasCharacter(DiscordMessage message, DiscordUser user)
        {
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker
            return turnTracker.Teams.Exists(team => team.Characters.Exists(cha => cha.PlayerID != null && cha.PlayerID.Equals(user.Id)));
        }
        /// <summary>
        /// Checks to see if a <see cref="DiscordUser"/> is the director for the Turn Tracker associated with the <see cref="DiscordMessage"/>.
        /// </summary>
        /// <param name="message">A <see cref="DiscordMessage"/> with a Turn Tracker embed.</param>
        /// <param name="user">The <see cref="DiscordUser"/> to check director status for.</param>
        /// <returns></returns>
        internal bool UserIsDirector(DiscordMessage message, DiscordUser user)
        {
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker
            return turnTracker.DirectorId.Equals(user.Id);
        }
        /// <summary>
        /// Function to handle changing the team of a player cha for a Turn Tracker.
        /// </summary>
        /// <param name="message">The original <see cref="DiscordMessage"/> of the Turn Tracker <see cref="DiscordEmbed"/>.</param>
        /// <param name="user">The interacting <see cref="DiscordUser"/> to change teams for.</param>
        /// <param name="optionId">The selected option Id the interacting <see cref="DiscordUser"/> chose.</param>
        /// <returns>A <see cref="DiscordWebhookBuilder"/> of the updated Turn Tracker and <see cref="DiscordComponent"/>s.</returns>
        internal DiscordWebhookBuilder HandlePlayerTeamChange(DiscordMessage message, DiscordUser user, string optionId)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            // check to see if 25 characters

            RemovePlayerCharacterFromTeams(user, turnTracker);

            if (!optionId.Equals("tts_dropdown_leave")) // The user has chosen to sign up for a specific team
            {
                AddPlayerCharacterToTeam(user, turnTracker, optionId);
            }

            return UpdateTurnTracker(builder, turnTracker);
        }
        internal DiscordWebhookBuilder HandlePlayerTurnToggle(DiscordMessage message, DiscordUser user)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            var character = turnTracker.Teams
                .SelectMany(team => team.Characters)
                .Single(cha => cha.PlayerID != null && cha.PlayerID.Equals(user.Id));

            character.TurnAvailable = !character.TurnAvailable;

            return UpdateTurnTracker(builder, turnTracker);
        }
        internal DiscordWebhookBuilder HandlePlayerReactionCycle(DiscordMessage message, DiscordUser user, string componentId)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            var character = turnTracker.Teams
                .SelectMany(team => team.Characters)
                .Single(cha => cha.PlayerID != null && cha.PlayerID.Equals(user.Id));

            if (componentId.Equals("tts_button_reaction_cycle"))
            {
                character.ReactionsAvailable = character.ReactionsAvailable > 0
                    ? character.ReactionsAvailable - 1
                    : character.ReactionsMax;
            }
            else if (componentId.Equals("tts_button_reaction_max"))
            {
                character.ReactionsMax = character.ReactionsMax == 1 ? 2 : 1;
                character.ReactionsAvailable = Math.Min(character.ReactionsAvailable, character.ReactionsMax);
            }

            return UpdateTurnTracker(builder, turnTracker);
        }
        internal DiscordInteractionResponseBuilder HandleDirectorAddCharactersSelection(string optionId)
        {
            var response = new DiscordInteractionResponseBuilder()
                .WithCustomId($"tts_modal_director_characters_{Util.ParseInt(optionId)}")
                .WithTitle(Resources.TurnTracker.NewNPCModalTitle);

            for (int i = 0; i < 5; i++)
            {
                response.AddComponents(new DiscordTextInputComponent(
                    label: Resources.TurnTracker.NewNPCModalComponentLabel,
                    customId: $"{i}",
                    placeholder: Resources.TurnTracker.NewNPCModalComponentPlaceholder,
                    required: false));
            }

            return response;
        }
        internal DiscordWebhookBuilder HandleDirectorAddCharactersModal(DiscordMessage message, IReadOnlyDictionary<string, string> modalValues, string modalId)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            // Add New Characters to Specified Team
            int teamPos = Util.ParseInt(modalId);
            foreach (var kvp in modalValues)
            {
                var name = kvp.Value.Trim();
                foreach (string remove in ProhibitedLetters)
                {
                    name = name.Replace(remove, string.Empty);
                }

                if (string.IsNullOrEmpty(name))
                    continue;

                turnTracker.Teams[teamPos].Characters.Add(new()
                {
                    CharacterName = name,
                    PlayerID = null,
                    ReactionsAvailable = 1,
                    ReactionsMax = 1,
                    TurnAvailable = true,
                    SelectedByDirector = false
                });
            }
            return UpdateTurnTracker(builder, turnTracker);
        }
        internal DiscordWebhookBuilder HandleCharacterSelection(DiscordMessage message, string[] components)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            foreach (var ch in turnTracker.Teams.SelectMany(team => team.Characters))
            {
                ch.SelectedByDirector = false;
            }
            foreach (var comp in components)
            {
                var code = Util.MatchIntegers(comp);
                turnTracker.Teams[code[0]].Characters[code[1]].SelectedByDirector = true;
            }

            return UpdateTurnTracker(builder, turnTracker);
        }
    }
}