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
            new DiscordButtonComponent(style: DiscordButtonStyle.Success, customId: "tts_button_turn", label: "Toggle Turn", emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Primary, customId: "tts_button_reaction_cycle", label: "Cycle Available Reaction(s)", emoji: new DiscordComponentEmoji("🔃")),
            new DiscordButtonComponent(style: DiscordButtonStyle.Secondary, customId: "tts_button_reaction_max", label: "Cycle Max Reactions", emoji: new DiscordComponentEmoji("🔃")),
        ];
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
                .WithAuthor(name: "Turn Tracker", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
                .AddField("Current Controller", "Not Assigned", inline: true)
                .AddField("Director", (director?.Mention ?? "Not Assigned"), inline: true)
                .AddField("Rotation History (Turn 1)", "New turn", inline: false)
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
        /// <summary>
        /// Checks to see if a <see cref="DiscordUser"/> is a part of any team on the Turn Tracker.
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
        /// Function to handle changing the team of a player cha for a Turn Tracker.
        /// </summary>
        /// <param name="message">The original <see cref="DiscordMessage"/> of the Turn Tracker <see cref="DiscordEmbed"/>.</param>
        /// <param name="user">The interacting <see cref="DiscordUser"/> to change teams for.</param>
        /// <param name="optionId">The selected option Id the interacting <see cref="DiscordUser"/> chose.</param>
        /// <returns>A <see cref="DiscordWebhookBuilder"/> of the updated Turn Tracker and <see cref="DiscordComponent"/>s.</returns>
        internal DiscordWebhookBuilder HandleTeamChange(DiscordMessage message, DiscordUser user, string optionId)
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
        internal DiscordWebhookBuilder HandleTurnToggle(DiscordMessage message, DiscordUser user)
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
        internal DiscordWebhookBuilder HandleReactionCycle(DiscordMessage message, DiscordUser user, string componentId)
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