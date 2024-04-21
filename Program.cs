using Microsoft.Extensions.Options;
using MO7.Commands;
using MO7.Events;
using MO7.Interactions;
using MO7.Options;
using MO7.Services;
using MO7.Tools;
using Modio;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .UseConsoleLifetime()
    .UseSerilog((context, services, configuration) =>
    {
        IOptions<SeqOptions> seqOptions = services.GetRequiredService<IOptions<SeqOptions>>();

        configuration
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Source", "MO7")
            .MinimumLevel.Debug()
            .WriteTo.Seq(seqOptions.Value.Url, apiKey: seqOptions.Value.Key)
            .WriteTo.Console();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<DiscordOptions>(context.Configuration.GetSection(DiscordOptions.SECTION));
        services.Configure<ModioOptions>(context.Configuration.GetSection(ModioOptions.SECTION));
        services.Configure<SeqOptions>(context.Configuration.GetSection(SeqOptions.SECTION));
        services.AddSingleton<EventRepository>();
        services.AddSingleton<ToolRepository>();
        services.AddSingleton(CreateClient);
        services.AddSingleton(CreateGameClient);
        services.AddSingleton(CreateModsClient);
        services.AddDiscordService(GetDiscordToken);
        services.AddHostedService<ModioService>();
        services.AddHostedService<DiscordService>();
        services.AddHostedService<SlashCommandsEnablerService>();
        services.AddDiscordCommands(true, true, true);
        services.AddCommandTree().WithCommandGroup<ToolCommandGroup>().Finish();
        services.AddInteractivity();
        services.AddInteractionGroup<ModalInteractions>();
    })
    .Build();

host.Run();
return;

static Client CreateClient(IServiceProvider serviceProvider)
{
    IOptions<ModioOptions> modioOptions = serviceProvider.GetRequiredService<IOptions<ModioOptions>>();
    Uri apiUrl = modioOptions.Value.TestEnvironment ? Client.ModioApiTestUrl : Client.ModioApiUrl;
    return new Client(apiUrl, new Credentials(modioOptions.Value.Key));
}

static GameClient CreateGameClient(IServiceProvider serviceProvider)
{
    Client client = serviceProvider.GetRequiredService<Client>();
    IOptions<ModioOptions> options = serviceProvider.GetRequiredService<IOptions<ModioOptions>>();
    return client.Games[options.Value.GameId];
}

static ModsClient CreateModsClient(IServiceProvider serviceProvider)
{
    GameClient gameClient = serviceProvider.GetRequiredService<GameClient>();
    return gameClient.Mods;
}

static string GetDiscordToken(IServiceProvider serviceProvider)
{
    IOptions<DiscordOptions> discordOptions = serviceProvider.GetRequiredService<IOptions<DiscordOptions>>();
    return discordOptions.Value.Token;
}
