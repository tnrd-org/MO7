using JetBrains.Annotations;
using MO7.Tools;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Results;

namespace MO7.Interactions;

public class ModalInteractions : InteractionGroup
{
    private readonly IInteractionCommandContext _interactionContext;
    private readonly ToolRepository _toolRepository;

    public ModalInteractions(IInteractionCommandContext interactionContext, ToolRepository toolRepository)
    {
        _interactionContext = interactionContext;
        _toolRepository = toolRepository;
    }

    [Modal("tool-update-modal")]
    [UsedImplicitly]
    public Task<Result> OnModalSubmittedAsync(
        string toolName,
        string toolVersion,
        string toolChangelog,
        string? toolWebsiteUrl = null,
        string? toolDownloadUrl = null)
    {

        _toolRepository.Add(new ToolData(toolName,
            toolVersion,
            toolChangelog,
            toolWebsiteUrl,
            toolDownloadUrl,
            _interactionContext.Interaction));

        return Task.FromResult(Result.FromSuccess());
    }
}
