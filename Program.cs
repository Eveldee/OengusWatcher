using Discord.Webhook;
using Microsoft.Extensions.Options;
using OengusWatcher;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Options
builder.Services
    .AddOptionsWithValidateOnStart<OengusWatcherOptions>()
    .Bind(builder.Configuration.GetSection(OengusWatcherOptions.ConfigurationSectionPath))
    .ValidateDataAnnotations();

// Services
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<HttpClient>();

builder.Services.AddSingleton<DiscordWebhookClient>(services =>
    new DiscordWebhookClient(
        services.GetRequiredService<IOptions<OengusWatcherOptions>>().Value.DiscordWebHook
    )
);

builder.Services.AddSingleton<ConnectionMultiplexer>(servies =>
    ConnectionMultiplexer.Connect(
        servies.GetRequiredService<IOptions<OengusWatcherOptions>>().Value.RedisConnectionString
    )
);

var host = builder.Build();
host.Run();