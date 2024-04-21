namespace MO7.Tools;

public class ToolRepository
{
    private readonly ILogger<ToolRepository> logger;
    private readonly AutoResetEvent locker = new AutoResetEvent(true);
    private readonly Queue<ToolData> events = new();

    public ToolRepository(ILogger<ToolRepository> logger)
    {
        this.logger = logger;
    }

    public void Add(ToolData toolData)
    {
        locker.WaitOne();

        try
        {
            events.Enqueue(toolData);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to add data");
        }
        finally
        {
            locker.Set();
        }
    }

    public IReadOnlyList<ToolData> GetData()
    {
        locker.WaitOne();

        try
        {
            List<ToolData> currentEvents = events.ToList();
            events.Clear();
            return currentEvents;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get data");
            return Array.Empty<ToolData>();
        }
        finally
        {
            locker.Set();
        }
    }
}
