using Remora.Discord.API.Abstractions.Objects;

namespace MO7.Tools;

public class ToolData
{
    public ToolData(string name,
        string version,
        string changelog,
        string? websiteUrl,
        string? downloadUrl,
        IInteraction interaction)
    {
        Name = name;
        Version = version;
        Changelog = changelog;
        WebsiteUrl = websiteUrl;
        DownloadUrl = downloadUrl;
        Interaction = interaction;
    }

    public string Name { get; }
    public string Version { get; }
    public string Changelog { get; }
    public string? WebsiteUrl { get; }
    public string? DownloadUrl { get; }
    public IInteraction Interaction { get; }
}
