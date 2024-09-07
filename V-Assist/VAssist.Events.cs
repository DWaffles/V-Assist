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
            if (e.Id.StartsWith("npt"))
                await HandleNarrativePointTrackerInteractionsAsync(client, e);
            else if (e.Id.StartsWith("tts"))
                await HandleTurnTrackerInteractions(client, e);
        }
        internal async Task ModalSubmittedEvent(DiscordClient Client, ModalSubmittedEventArgs e)
        {
            if (e.Interaction.Data.CustomId == "modal_npt")
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Interaction.Message;
                var service = (NarrativePointTrackerService)Client.ServiceProvider.GetRequiredService(typeof(NarrativePointTrackerService));

                var webhookBuilder = service.HandleTrackerModal(message, e.Interaction.User, e.Values);
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