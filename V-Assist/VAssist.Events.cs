using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using VAssist.Common;
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
            
            // convert to switch function?
            if (new List<string> { "npt_spend", "npt_add", "npt_end", "npt_rgm", "npt_bgm" }.Contains(e.Id)) // split up logic into various functions
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                if (e.Id == "npt_end")
                {
                    if (service.AllowDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleTrackerEnd(message);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You are not the director of the session and cannot end the session.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                }
                else if (service.EmbedMaxFields(embed)) // check to see if the embed fields are full before attempting to add a field.
                {
                    await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "Due to Discord limits, this session cannot have any more narrative point changes." });
                }
                else if (e.Id == "npt_spend")
                {
                    var webhookBuilder = service.HandlePointSpend(message, e.User);
                    await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                }
                else if(e.Id == "npt_add")
                {
                    if(service.AllowDirectorAction(message, e.User)) // check if the user can add a point
                    {
                        var webhookBuilder = service.HandlePointAdd(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You are not the director of the session and cannot add narrative points to the party.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                }
                else if (e.Id == "npt_rgm") // resign director
                {
                    if (service.AllowDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleDirectorResign(message);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else // don't allow
                    {
                        string content = "You are not the director.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                }
                else if (e.Id == "npt_bgm") // become director
                {
                    if (service.AllowDirectorAction(message, e.User))
                    {
                        var webhookBuilder = service.HandleDirectorAssign(message, e.User);
                        await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    }
                    else
                    {
                        string content = "You cannot become the director.";
                        await e.Interaction.CreateFollowupMessageAsync(new() { Content = content, IsEphemeral = true });
                    }
                }
            }
            
            if (e.Id == "npt_for")
            {
                var builder = service.HandleAddReason(message, e.User);
                if(!builder.Components.Any())
                {
                    await e.Interaction.CreateFollowupMessageAsync( new() { IsEphemeral = true, Content = "You don't have any changes." });
                }
                else
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, builder);
                }
            }
        }
        internal async Task ModalSubmittedEvent(DiscordClient Client, ModalSubmittedEventArgs e)
        {
            if(e.Interaction.Data.CustomId == "modal_npt")
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Interaction.Message;
                var fields = message.Embeds[0].Fields?.ToList();
                var embed = new DiscordEmbedBuilder(message.Embeds[0]).ClearFields();
                foreach (var field in fields)
                {
                    Util.ParseNarrativePointFieldValue(field.Value, out ulong unixTime, out string? reason);

                    if (e.Values.ContainsKey(unixTime.ToString()))
                    {
                        string? value = e.Values[unixTime.ToString()];
                        if (!string.IsNullOrEmpty(value))
                            embed.AddField(field.Name, $"By {e.Interaction.User.Mention} @ <t:{unixTime}:t>. Reason: {e.Values[unixTime.ToString()]}");
                        else
                            embed.AddField(field.Name, $"By {e.Interaction.User.Mention} @ <t:{unixTime}:t>");
                    }
                    else
                    {
                        embed.AddField(field.Name, field.Value);
                    }
                }

                var buttonRows = message.Components.Select(row => row.Components.ToList()).ToList();
                var webhookBuilder = new DiscordWebhookBuilder()
                    .AddEmbed(embed)
                    .AddComponents(buttonRows.First())
                    .AddComponents(buttonRows.Last());

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