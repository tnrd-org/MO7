using DataSizeUnits;
using Microsoft.Extensions.Options;
using MO7.Events;
using MO7.Options;
using Modio.Models;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Extensions.Embeds;
using Remora.Rest.Core;
using Remora.Results;

namespace MO7.Services;

public class DiscordService : BackgroundService
{
    private class Target
    {
        public readonly string Tag;
        public readonly Snowflake ChannelSnowflake;
        public readonly Snowflake RoleSnowflake;

        public Target(string tag, string channelSnowflake, string roleSnowflake)
        {
            Tag = tag;

            if (Snowflake.TryParse(channelSnowflake, out Snowflake? temp))
                ChannelSnowflake = temp.Value;
            if (Snowflake.TryParse(roleSnowflake, out temp))
                RoleSnowflake = temp.Value;
        }

        public bool IsValidForTag(Mod mod)
        {
            return mod.Tags.Any(x => Tag.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase));
        }
    }

    private const string AUTHOR_NAME = "Zeepkist";
    private const string AUTHOR_URL = "https://mod.io/g/zeepkist";
    private const string AUTHOR_ICON = "https://thumb.modcdn.io/games/9bd5/3213/crop_64x64/zeepkist-icon-1024x1024.png";

    private readonly EventRepository eventRepository;
    private readonly IDiscordRestChannelAPI channelApi;
    private readonly ILogger<DiscordService> logger;

    private readonly List<Target> targets = new();

    public DiscordService(
        EventRepository eventRepository,
        ILogger<DiscordService> logger,
        IDiscordRestChannelAPI channelApi,
        IOptions<DiscordOptions> discordOptions
    )
    {
        this.eventRepository = eventRepository;
        this.logger = logger;
        this.channelApi = channelApi;

        DiscordOptions options = discordOptions.Value;

        targets.Add(new Target(options.BlueprintTag,
            options.BlueprintChannelSnowflake,
            options.BlueprintRoleSnowflake));

        targets.Add(new Target(options.ModTag,
            options.ModChannelSnowflake,
            options.ModRoleSnowflake));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<EventData> events = eventRepository.GetEvents();
            logger.LogInformation("Processing {Count} events", events.Count);

            foreach (EventData eventData in events)
            {
                await ProcessEvent(eventData, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessEvent(EventData eventData, CancellationToken stoppingToken)
    {
        foreach (Target target in targets)
        {
            if (!target.IsValidForTag(eventData.Mod))
                continue;

            IEmbed embed = eventData.EventType switch
            {
                EventType.Available => CreateAvailableEmbed(eventData.Mod, target.RoleSnowflake, target.Tag),
                EventType.Update => CreateUpdateEmbed(eventData.Mod, target.RoleSnowflake, target.Tag),
                _ => throw new ArgumentOutOfRangeException()
            };

            Result<IMessage> messageResult = await channelApi.CreateMessageAsync(target.ChannelSnowflake,
                embeds: new List<IEmbed>()
                {
                    embed
                },
                ct: stoppingToken);

            if (messageResult.IsSuccess)
            {
                logger.LogInformation("Successfully sent message");
            }
            else
            {
                logger.LogError("Failed to send message: {Result}", messageResult.Error.ToString());
            }
        }
    }

    private IEmbed CreateAvailableEmbed(Mod mod, Snowflake roleSnowflake, string tag)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(AUTHOR_NAME, AUTHOR_URL, AUTHOR_ICON)
            .WithTitle(mod.Name!)
            .WithUrl(mod.ProfileUrl!.ToString())
            .WithDescription($"A new {tag.ToLower()} is available.")
            .WithFooter(mod.SubmittedBy!.Username!, mod.SubmittedBy!.Avatar!.Thumb50x50!.ToString());

        if (mod.Media.Images.Count > 0)
        {
            Image img = mod.Media.Images.First();
            embedBuilder = embedBuilder.WithImageUrl(img.Thumb320x180!.ToString());

            if (mod.Logo != null)
            {
                embedBuilder = embedBuilder.WithThumbnailUrl(mod.Logo.Thumb320x180!.ToString());
            }
        }
        else
        {
            embedBuilder = embedBuilder.WithImageUrl(mod.Logo!.Thumb320x180!.ToString());
        }

        embedBuilder.AddField("Description", mod.Summary!);
        embedBuilder.AddField("Info",
            $"Links: [Download]({mod.Modfile!.Download!.BinaryUrl})\n" +
            $"Version: {mod.Modfile.Version}\n" +
            $"Size: {new DataSize(mod.Modfile.FileSize, Unit.Byte).Normalize()}",
            true);
        embedBuilder.AddField("Tags", string.Join(", ", mod.Tags.Select(x => x.Name)), true);
        embedBuilder.AddField("Mentions", $"<@&{roleSnowflake}>");

        Result<Embed> result = embedBuilder.Build();

        if (result.IsSuccess)
            return result.Entity;

        throw new InvalidOperationException("Failed to create embed");
    }

    private IEmbed CreateUpdateEmbed(Mod mod, Snowflake roleSnowflake, string tag)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(AUTHOR_NAME, AUTHOR_URL, AUTHOR_ICON)
            .WithTitle(mod.Name!)
            .WithUrl(mod.ProfileUrl!.ToString())
            .WithThumbnailUrl(mod.Logo!.Thumb320x180!.ToString())
            .WithDescription(GetDescription())
            .WithFooter(mod.SubmittedBy!.Username!, mod.SubmittedBy!.Avatar!.Thumb50x50!.ToString());

        if (mod.Modfile != null && !string.IsNullOrEmpty(mod.Modfile.Changelog))
        {
            embedBuilder.AddField("Changelog", mod.Modfile.Changelog);
        }

        embedBuilder.AddField("Mentions", $"<@&{roleSnowflake}>");

        Result<Embed> result = embedBuilder.Build();

        if (result.IsSuccess)
            return result.Entity;

        throw new InvalidOperationException("Failed to create embed");

        string GetDescription()
        {
            if (mod.Modfile == null)
            {
                logger.LogWarning("A new mod version is available but there is no mod file? {ModId}", mod.Id);
                return "A new version is available.";
            }

            return string.IsNullOrEmpty(mod.Modfile.Version)
                ? "A new version is available."
                : $"A new version is available. [Version {mod.Modfile.Version}]({mod.Modfile!.Download!.BinaryUrl})";
        }
    }
}
