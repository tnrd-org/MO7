namespace MO7.Options;

public class ModioOptions
{
    public const string SECTION = "Modio";

    public bool TestEnvironment { get; set; }
    public string Key { get; set; } = null!;
    public uint GameId { get; set; }
}
