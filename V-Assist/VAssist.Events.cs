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
            if (new List<string> { "npt_spend", "npt_add", "npt_end" }.Contains(e.Id)) // split up logic into various functions
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Message;
                var embed = new DiscordEmbedBuilder(message.Embeds[0]);
                var service = (NarrativePointTrackerService)Client.ServiceProvider.GetRequiredService(typeof(NarrativePointTrackerService));

                if (e.Id == "npt_end")
                {
                    var webhookBuilder = service.HandlePointTrackerEnd(message);
                    await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    return;
                }
                else if (service.EmbedMaxFields(embed))
                {
                    await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "Due to Discord limits, this session cannot have any more narrative point changes." });
                    return;
                }
                else if (e.Id == "npt_spend")
                {
                    var webhookBuilder = service.HandlePointTrackerSpend(message, e.User);
                    await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    return;
                }
                else if(e.Id == "npt_add")
                {
                    if(service.TrackerGMCheck())
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "You are not the director of the session and cannot add narrative points to the party." });
                        return;
                    }
                    var webhookBuilder = service.HandlePointTrackerAdd(message, e.User);
                    await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
                    return;
                }
            }
            else if (e.Id == "npt_bgm" || e.Id == "npt_rgm")
            {

            }
            else if (e.Id == "npt_for")
            {
                var fields = e.Message.Embeds[0].Fields?.ToList();
                fields.RemoveAt(0);
                fields = fields.Where(f => f.Value.Contains(e.User.Mention)).ToList();
                if (fields.Count > 5)
                {
                    fields = fields.GetRange(fields.Count - 5, 5);
                }
                else if (fields.Count == 0)
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new() { IsEphemeral = true, Content = "You don't have any changes." });
                    return;
                }

                var builder = new DiscordInteractionResponseBuilder()
                    .WithCustomId("modal_npt")
                    .WithTitle("Add Reason");

                foreach (var field in fields)
                {
                    Util.ParseNarrativePointFieldValue(field.Value, out ulong unixTime, out string? reason);
                    builder.AddComponents(new DiscordTextInputComponent(label: $"{field.Name} by You", customId: unixTime.ToString(), value: reason, required: false));
                }

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, builder);
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