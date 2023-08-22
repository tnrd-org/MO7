namespace MO7.Options;

public class DiscordOptions
{
    public const string SECTION = "Discord";
    
    public string Token { get; set; } = null!;
    public string BlueprintTag { get; set; } = null!;
    public string BlueprintChannelSnowflake { get; set; } = null!;
    public string BlueprintRoleSnowflake { get; set; } = null!;
    public string ModTag { get; set; } = null!;
    public string ModChannelSnowflake { get; set; } = null!;
    public string ModRoleSnowflake { get; set; } = null!;
}
