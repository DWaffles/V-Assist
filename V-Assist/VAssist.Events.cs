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
            var message = e.Message;
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var service = (NarrativePointTrackerService)Client.ServiceProvider.GetRequiredService(typeof(NarrativePointTrackerService));

            if (e.Id == "npt_for")
            {
                var builder = service.HandleAddReason(message, e.User);
                if (!builder.Components.Any())
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                    await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "You don't have any changes." });
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, builder);
                }
            }

            switch (e.Id) // split up logic into various functions
            {
                case "npt_spend":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.CheckEmbedMaxFields(embed)) // check to see if the embed fields are full before attempting to add a field.
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "Due to Discord limits, this session cannot have any more narrative point changes." });
                    }
                    else
                    {
                        var webhookBuilder = service.HandlePointSpend(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    break;
                case "npt_add":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.CheckEmbedMaxFields(embed)) // check to see if the embed fields are full before attempting to add a field.
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "Due to Discord limits, this session cannot have any more narrative point changes." });
                    }
                    else if (service.CheckDirectorAction(message, e.User)) // check if the user can add a point
                    {
                        var webhookBuilder = service.HandlePointAdd(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You are not the director of the session and cannot add narrative points to the party.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
                case "npt_end":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.CheckDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleTrackerEnd(message);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You are not the director of the session and cannot end the session.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
                case "npt_bgm":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.CheckDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleDirectorAssign(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You cannot become the director.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
                case "npt_rgm":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.CheckDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleDirectorResign(message);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else // don't allow
                    {
                        string content = "You are not the director.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
            }
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