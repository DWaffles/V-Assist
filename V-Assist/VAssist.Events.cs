using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using VAssist.Services;

namespace VAssist
{
    internal partial class VAssist
    {
        internal Task GuildAvailableEvent(DiscordClient client, GuildAvailableEventArgs e)
        {
            Log.Logger.Information($"Guild Available; Name: {e.Guild.Name}; ID: {e.Guild.Id}");
            return Task.CompletedTask;
        }
        internal async Task ComponentInteractionCreatedEvent(DiscordClient client, ComponentInteractionCreatedEventArgs e)
        {
            Log.Logger.Information($"Processing interaction with component ID: {e.Id} from user: {e.User.Id}");
            if (e.Id.StartsWith("npt"))
                await HandleNarrativePointTrackerInteractionsAsync(client, e);
            else if (e.Id.StartsWith("tts"))
                await HandleTurnTrackerInteractionsAsync(client, e);
        }
        internal async Task ModalSubmittedEvent(DiscordClient Client, ModalSubmittedEventArgs e)
        {
            Log.Logger.Information($"Processing modal submitted with ID: {e.Interaction.Data.CustomId} by user: {e.Interaction.User.Id}");

            var modalId = e.Interaction.Data.CustomId;
            if (modalId.Equals("modal_npt"))
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Interaction.Message;
                var service = (NarrativePointTrackerService)Client.ServiceProvider.GetRequiredService(typeof(NarrativePointTrackerService));

                var webhookBuilder = service.HandleTrackerModal(message, e.Interaction.User, e.Values);
                await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
            }
            else if (modalId.StartsWith("tts_modal_director_characters"))
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Interaction.Message;
                var service = (TurnTrackerService)Client.ServiceProvider.GetRequiredService(typeof(TurnTrackerService));

                var webhookBuilder = service.HandleDirectorAddCharactersModal(message, e.Values, modalId);
                await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
            }
        }
        internal Task ZombiedEvent(DiscordClient client, ZombiedEventArgs e)
        {
            Log.Logger.Information($"Client has zombied.");
            return Task.CompletedTask;
        }
    }
}