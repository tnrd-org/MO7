using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;

namespace MO7.Commands;

[Group("tool")]
public partial class ToolCommandGroup : CommandGroup
{
    private readonly IDiscordRestInteractionAPI _interactionApi;
    private readonly IInteractionCommandContext _interactionContext;

    public ToolCommandGroup(IDiscordRestInteractionAPI interactionApi, IInteractionCommandContext interactionContext)
    {
        _interactionApi = interactionApi;
        _interactionContext = interactionContext;
    }
}
