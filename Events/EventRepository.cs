using Modio.Models;

namespace MO7.Events;

public class EventRepository
{
    private readonly ILogger<EventRepository> logger;
    private readonly AutoResetEvent locker = new AutoResetEvent(true);
    private readonly Queue<EventData> events = new();

    public EventRepository(ILogger<EventRepository> logger)
    {
        this.logger = logger;
    }

    public void Add(EventType eventType, Mod mod)
    {
        locker.WaitOne();

        try
        {
            events.Enqueue(new EventData(eventType, mod));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add event");
        }
        finally
        {
            locker.Set();
        }
    }

    public IReadOnlyList<EventData> GetEvents()
    {
        locker.WaitOne();

        try
        {
            List<EventData> currentEvents = events.ToList();
            events.Clear();
            return currentEvents;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get events");
            return Array.Empty<EventData>();
        }
        finally
        {
            locker.Set();
        }
    }
}
