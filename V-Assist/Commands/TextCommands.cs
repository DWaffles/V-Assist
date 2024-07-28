using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;

namespace VAssist.Commands
{
    internal class TextCommands
    {
        [Command("ping")]
        public static ValueTask PingAsync(TextCommandContext ctx) => ctx.RespondAsync($"Pong! {ctx.Client.GetConnectionLatency(ctx.Guild?.Id ?? 0).TotalMilliseconds} ms.");

    }
}
