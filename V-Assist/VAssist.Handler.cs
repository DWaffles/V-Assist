using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using VAssist.Services;

namespace VAssist
{
    internal partial class VAssist
    {
        internal async Task HandleNarrativePointTrackerInteractionsAsync(DiscordClient client, ComponentInteractionCreatedEventArgs e)
        {
            var message = e.Message;
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var service = (NarrativePointTrackerService)Client.ServiceProvider.GetRequiredService(typeof(NarrativePointTrackerService));

            switch (e.Id) // split up logic into various functions // npt
            {
                case "npt_for":
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
                    break;
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
                default:
                    throw new NotSupportedException();
            }
        }
        internal async Task HandleTurnTrackerInteractions(DiscordClient client, ComponentInteractionCreatedEventArgs e)
        {
            var message = e.Message;
            var embed = new DiscordEmbedBuilder(message.Embeds[0]);
            var service = (TurnTrackerService)Client.ServiceProvider.GetRequiredService(typeof(TurnTrackerService));

            switch (e.Id) // tts
            {
                case "tts_dropdown":
                    if (service.UserIsDirector(message, e.User)) //check controller status
                    {

                    }
                    else
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                        var webhookBuilder = service.HandlePlayerTeamChange(message, e.User, e.Values.First());
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    break;
                case "tts_button_turn":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.UserIsDirector(message, e.User)) //check controller status
                    {

                    }
                    else if (service.UserHasCharacter(message, e.User)) // Check if user is in a team
                    {
                        var webhookBuilder = service.HandlePlayerTurnToggle(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You do not have a character for this Turn Tracker.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
                case "tts_button_reaction_cycle":
                    goto case "tts_button_reaction_max";
                case "tts_button_reaction_max":
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (service.UserIsDirector(message, e.User)) //check controller status
                    {

                    }
                    else if (service.UserHasCharacter(message, e.User)) // Check if user is in a team
                    {
                        var webhookBuilder = service.HandlePlayerReactionCycle(message, e.User, e.Id);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You do not have a character for this Turn Tracker.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}