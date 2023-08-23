using MO7.Events;
using Modio;
using Modio.Filters;
using Modio.Models;

namespace MO7.Services;

public class ModioService : BackgroundService
{
    [Flags]
    private enum CustomFlags
    {
        None = 0,
        Available = 1 << 0,
        Deleted = 1 << 1,
        Updated = 1 << 2
    }

    private readonly EventRepository eventRepository;
    private readonly ModsClient modsClient;
    private readonly ILogger<ModioService> logger;

    public ModioService(ILogger<ModioService> logger, ModsClient modsClient, EventRepository eventRepository)
    {
        this.logger = logger;
        this.modsClient = modsClient;
        this.eventRepository = eventRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long lastCheckedStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Checking for new events");

            IReadOnlyList<ModEvent> events;
            
            try
            {
                SearchClient<ModEvent> searchClient = modsClient.GetEvents(
                    Filter.Custom("date_added", Operator.GreaterThan, lastCheckedStamp.ToString()));

                events = await searchClient.ToList();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get events");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            List<IGrouping<uint, ModEvent>> eventsGroupedByMod = events.GroupBy(x => x.ModId).ToList();

            foreach (IGrouping<uint, ModEvent> group in eventsGroupedByMod)
            {
                await ProcessEventGroup(group);
            }

            logger.LogInformation("Waiting for next check");
            lastCheckedStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessEventGroup(IGrouping<uint, ModEvent> group)
    {
        uint modId = group.Key;

        List<ModEvent> modEvents = group.Reverse().ToList();

        if (!TryGetFlagsFromEvents(modEvents, out CustomFlags importantEvents))
            return;

        Mod mod;
        
        try
        {
            mod = await modsClient[modId].Get();
        }
        catch (ForbiddenException)
        {
            logger.LogWarning("Trying to access a mod ({ModId}) that we do not have access to!", modId);
            return;
        }

        if (importantEvents.HasFlag(CustomFlags.Available))
        {
            logger.LogInformation("Published mod available");
            eventRepository.Add(EventType.Available, mod);
        }
        else if (importantEvents.HasFlag(CustomFlags.Updated))
        {
            logger.LogInformation("Published mod update");
            eventRepository.Add(EventType.Update, mod);
        }
    }

    private static bool TryGetFlagsFromEvents(List<ModEvent> modEvents, out CustomFlags importantEvents)
    {
        importantEvents = CustomFlags.None;

        foreach (ModEvent modEvent in modEvents)
        {
            switch (modEvent.EventType)
            {
                case ModEventType.MODFILE_CHANGED:
                    importantEvents |= CustomFlags.Updated;
                    break;
                case ModEventType.MOD_AVAILABLE:
                    importantEvents |= CustomFlags.Available;
                    break;
                case ModEventType.MOD_UNAVAILABLE:
                    importantEvents &= ~CustomFlags.Available;
                    break;
                case ModEventType.MOD_DELETED:
                    importantEvents |= CustomFlags.Deleted;
                    importantEvents &= ~CustomFlags.Available;
                    break;
            }
        }

        return importantEvents != CustomFlags.None;
    }
}
