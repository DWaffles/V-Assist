using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
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
        /// List of default team names to use for a new turn tracker.
        /// </summary>
        internal static List<string> DefaultTeamNames { get; } = ["Team A", "Team B", "Team C", "Team D", "Team E", "Team F"];
        internal static DiscordComponent[] TurnTrackerRowOne { get; } = [
            new DiscordButtonComponent(style: DiscordButtonStyle.Success, customId: "tts_button_turn", label: Resources.TurnTracker.ButtonToggleTurnLabel, emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Primary, customId: "tts_button_reaction_cycle", label: Resources.TurnTracker.ButtonReactionCycleLabel, emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "tts_button_reaction_max", label: Resources.TurnTracker.ButtonReactionMaxLabel, emoji: new DiscordComponentEmoji("🔃")),
        ];
        /// <summary>
        /// Gets a <see cref="DiscordEmbed"/> representing a new turn tracker.
        /// </summary>
        /// <param name="ctx">The base context for slash command contexts.</param>
        /// <param name="num_teams">The number of teams to include in the new turn tracker.</param>
        /// <param name="director">The <see cref="DiscordUser"/> who will serve as the director for the turn tracker.</param>
        /// <returns>A <see cref="DiscordEmbed"/> representing a new turn tracker.</returns>
        internal DiscordEmbed GetNewEmbed(SlashCommandContext ctx, int num_teams, DiscordUser director)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor(name: Resources.TurnTracker.AuthorName, iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .AddField(Resources.TurnTracker.ControllerFieldName, Resources.TurnTracker.ControllerFieldValueDefault, inline: true)
                .AddField(Resources.TurnTracker.DirectorFieldName, director.Mention, inline: true)
                .AddField(Resources.TurnTracker.RotationFieldNamePrefix + " (Turn 1)", Resources.TurnTracker.RotationFieldValueDefault, inline: false)
                .WithFooter(text: ctx.Client.CurrentUser.Username + " • " + Resources.TurnTracker.TurnTrackerCurrentVersion)
                .WithTimestamp(DateTime.Now)
                .WithColor(new DiscordColor("bc2019"));

            for (int i = 0; i < num_teams && i < DefaultTeamNames.Count; i++) // Add the number of specified teams to the turn tracker
            {
                embed.AddField(DefaultTeamNames[i], Resources.TurnTracker.TeamFieldValueDefault, inline: true);
            }

            embed.AddField(Resources.TurnTracker.KeyFieldName,  $"{Green} = {Resources.TurnTracker.GreenDescription}" +
                                                                $"\n{Blue} = {Resources.TurnTracker.BlueDescription}" +
                                                                $"\n{Orange} = {Resources.TurnTracker.OrangeDescription}" +
                                                                $"\n{Red} = {Resources.TurnTracker.RedDescription}", inline: false);
            embed.AddField(Resources.TurnTracker.DirectorCharacterFieldName, Resources.TurnTracker.DirectorCharacterFieldValueDefault);
            return embed.Build();
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

            RemovePlayerCharacterFromTeams(user, turnTracker);

            if (!optionId.Equals("tts_dropdown_leave")) // The user has chosen to sign up for a specific team
            {
                AddPlayerCharacterToTeam(user, turnTracker, optionId);
            }

            UpdateTurnTracker(builder, turnTracker);
            return new DiscordWebhookBuilder()
               .AddEmbed(builder)
               .AddComponents(message.Components);
        }
        internal DiscordWebhookBuilder HandlePlayerTurnToggle(DiscordMessage message, DiscordUser user)
        {
            var builder = new DiscordEmbedBuilder(message.Embeds[0]); // put the turn tracker in an builder builder to be able to edit it
            var turnTracker = ParseTurnTracker(message.Embeds[0]); // parse the changable details of the turn tracker

            var character = turnTracker.Teams
                .SelectMany(team => team.Characters)
                .Single(cha => cha.PlayerID != null && cha.PlayerID.Equals(user.Id));

            character.TurnAvailable = !character.TurnAvailable;

            UpdateTurnTracker(builder, turnTracker);
            return new DiscordWebhookBuilder()
               .AddEmbed(builder)
               .AddComponents(message.Components);
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

            UpdateTurnTracker(builder, turnTracker);
            return new DiscordWebhookBuilder()
               .AddEmbed(builder)
               .AddComponents(message.Components);
        }
    }
}