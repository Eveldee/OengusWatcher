using System.Net.Http.Json;
using Discord;
using Discord.Webhook;
using OengusWatcher.Models;
using StackExchange.Redis;

namespace OengusWatcher;

public class Worker : BackgroundService
{
    private const int CheckInterval = 3600 * 1000;
    private const string OengusMarathonsEndpoint = "https://oengus.io/api/v2/marathons/for-home";
    private const string MarathonsKey = "OengusWatcher:Marathons";

    private readonly ILogger<Worker> _logger;
    private readonly DiscordWebhookClient _discordClient;
    private readonly ConnectionMultiplexer _redis;
    private readonly HttpClient _httpClient;

    public Worker(ILogger<Worker> logger, DiscordWebhookClient discordClient, ConnectionMultiplexer redis, HttpClient httpClient)
    {
        _logger = logger;
        _discordClient = discordClient;
        _redis = redis;
        _httpClient = httpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Oengus watcher started");

        var database = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Fetching marathon list from api...");

            var marathons = await _httpClient.GetFromJsonAsync<MarathonsList>(OengusMarathonsEndpoint, cancellationToken: stoppingToken);

            if (marathons is not null)
            {
                _logger.LogInformation("Fetched a total of {Live} live marathons, {Next} next marathons, {Open} open marathons",
                    marathons.Live.Length,
                    marathons.Next.Length,
                    marathons.Open.Length
                );

                var openMarathons = marathons.Open;

                var newMarathons = new List<Marathon>();

                foreach (var marathon in openMarathons)
                {
                    // Check if marathon has already been processed
                    if (await database.SetContainsAsync(MarathonsKey, marathon.Id))
                    {
                        continue;
                    }

                    newMarathons.Add(marathon);

                    // Make sure this marathon won't be checked twice
                    await database.SetAddAsync(MarathonsKey, marathon.Id);
                }

                _logger.LogInformation("There is a total of {Count} new marathons since last check", newMarathons.Count);

                // Send discord messages if there is at least one new marathon
                if (newMarathons is not [])
                {
                    foreach (var embeds in BuildEmbeds(newMarathons).Chunk(10))
                    {
                        _logger.LogInformation("Sending discord message...");

                        await _discordClient.SendMessageAsync(embeds: embeds);
                    }
                }
            }
            else
            {
                _logger.LogError("Invalid response from the api, skipping...");
            }

            _logger.LogInformation("Check done, next check at {Date}", DateTime.Now.AddMilliseconds(CheckInterval));

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private static IEnumerable<Embed> BuildEmbeds(List<Marathon> newMarathons)
    {
        foreach (var marathon in newMarathons)
        {
            yield return new EmbedBuilder()
                .WithTitle(marathon.Name)
                .WithUrl($"https://oengus.io/marathon/{marathon.Id}")
                .WithColor(0x00C9FF)

                .WithFooter(new EmbedFooterBuilder()
                    .WithText("OengusWatcher")
                    .WithIconUrl("https://sage.cdn.ilysix.fr/assets/OengusWatcher/Oengus_Logo.webp")
                )

                .AddField("Start Date", ToTimeStamp(marathon.StartDate), true)
                .AddField("End Date", ToTimeStamp(marathon.EndDate), true)

                .AddField("Submission End Date", ToTimeStamp(marathon.SubmissionsEndDate))

                .AddField("Language", $"{ToFlag(marathon.Language)} {marathon.Language.ToUpperInvariant()}", true)
                .AddField("On Site", marathon.Onsite, true)

                .Build();
        }
    }

    private static string ToFlag(string language)
    {
        string code = language switch
        {
            "en" => "gb",
            "ja" => "jp",
            "zh" => "cn",
            "ch" => "cn",
            _ => language
        };

        return $":flag_{code}:";
    }

    private static string ToTimeStamp(DateTime? dateTime)
    {
        return dateTime is null
            ? "N/A"
            : TimestampTag.FormatFromDateTime(dateTime.Value, TimestampTagStyles.LongDateTime);
    }
}