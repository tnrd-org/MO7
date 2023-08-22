using Modio.Models;

namespace MO7.Events;

public class EventData
{
    public EventData(EventType eventType, Mod mod)
    {
        EventType = eventType;
        Mod = mod;
    }

    public EventType EventType { get; }
    public Mod Mod { get; }
}
