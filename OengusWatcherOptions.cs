using System.ComponentModel.DataAnnotations;

namespace OengusWatcher;

public sealed class OengusWatcherOptions
{
    public const string ConfigurationSectionPath = "OengusWatcher";

    [Required]
    [DeniedValues(null, "")]
    public required string DiscordWebHook { get; init; }
    [Required]
    [DeniedValues(null, "")]
    public required string RedisConnectionString { get; init; }
}