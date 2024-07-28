using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Serilog;
using VAssist.Common;

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
            if (e.Id == "npt_spend" || e.Id == "npt_add" || e.Id == "npt_end")
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                var message = e.Message;
                var embed = new DiscordEmbedBuilder(message.Embeds[0]);
                var buttonRows = message.Components.Select(row => row.Components.ToList()).ToList();

                if (e.Id == "npt_spend" || e.Id == "npt_add")
                {
                    if (embed.Fields.Count >= 25)
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new() { IsEphemeral = true, Content = "Due to Discord limits, this session cannot have any more narrative point changes." });
                        return;
                    }
                    buttonRows.Last().ForEach(button => button = ((DiscordButtonComponent)button).Enable()); // Enable reason button

                    int points_old = (int)Util.ParseInt(message.Embeds[0].Title ?? "-1"); // get old points
                    int points_new = string.Equals(e.Id, "npt_spend") ? points_old - 1 : points_old + 1; // get new points

                    embed.WithTitle($"Current Narrative Points: {points_new}"); // set new points
                    embed.AddField($"{points_old} -> {points_new}", $"By {e.User.Mention} @ <t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:t>"); // add field
                }
                else // e.Id == "npt_end"
                {
                    buttonRows.ForEach(row => row.ForEach(button => button = ((DiscordButtonComponent)button).Disable())); // disable all buttons
                }

                var webhookBuilder = new DiscordWebhookBuilder()
                    .AddEmbed(embed)
                    .AddComponents(buttonRows.First())
                    .AddComponents(buttonRows.Last());

                await e.Interaction.EditOriginalResponseAsync(webhookBuilder);
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