using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VAssist.Common;
using VAssist.Services;

namespace VAssist
{
    internal partial class VAssist
    {
        internal BotConfig Config { get; }
        internal IServiceCollection Services { get; }
        internal DiscordClient Client { get; }
        internal CommandsExtension Commands { get; }
        internal VAssist(BotConfig? config)
        {
            if (!FileHandler.VerifyConfig(config) || config == null)
            {
                throw new ArgumentException("Config does not contain a valid token and/or prefix.");
            }

            Config = config;

            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"data{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, shared: true)
                .CreateLogger();

            DiscordClientBuilder builder = DiscordClientBuilder.CreateDefault(Config.Token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents)
                .ConfigureLogging(logging => { logging.ClearProviders(); logging.AddSerilog(); })
                .ConfigureServices(services => services.AddSingleton<NarrativePointTrackerService>())
                .ConfigureEventHandlers(b => b
                    .HandleGuildAvailable(GuildAvailableEvent)
                    .HandleComponentInteractionCreated(ComponentInteractionCreatedEvent)
                    .HandleModalSubmitted(ModalSubmittedEvent)
                    .HandleZombied(ZombiedEvent));

            Client = builder.Build();

            Commands = Client.UseCommands(new CommandsConfiguration()
            {
                RegisterDefaultCommandProcessors = true,
                UseDefaultCommandErrorHandler = true,
            });
        }
        internal async Task Run()
        {
            await RegisterCommandsAsync();
            await ConnectAsync();
        }
        private async Task ConnectAsync()
        {
            var status = Config.Status ?? Config.CommandPrefixes[0] + "help"; // VerifyConfig() enforces at least 1 non-whitespace prefix.
            var activity = new DiscordActivity(status, DiscordActivityType.Watching);

            try
            {
                await Client.ConnectAsync(activity);
            }
            catch (Exception ex) // SystemException
            {
                Log.Error(ex, "Exception occured while connecting to Discord.");
            }

            await Task.Delay(-1);
        }
        private async Task RegisterCommandsAsync()
        {
            TextCommandProcessor processorText = new(new()
            {
                PrefixResolver = new DefaultPrefixResolver(true, Config.CommandPrefixes).ResolvePrefixAsync
            });
            SlashCommandProcessor processorSlash = new();

            await Commands.AddProcessorsAsync(processorText, processorSlash);

            Commands.AddCommands(typeof(Program).Assembly);
        }
    }
}