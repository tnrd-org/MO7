using JetBrains.Annotations;
using OneOf;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;

namespace MO7.Commands;

public partial class ToolCommandGroup
{
    [Ephemeral]
    [SuppressInteractionResponse(true)]
    [Command("update")]
    [UsedImplicitly]
    public async Task<Result> UpdateCommand()
    {
        InteractionModalCallbackData modal = new(
            CustomIDHelpers.CreateModalID("tool-update-modal"),
            "Tool Update Form",
            new[]
            {
                CreateField("tool-name", "Name", placeholder: "My Mod"),
                CreateField("tool-version", "Version", placeholder: "1.2.3"),
                CreateField("tool-changelog", "Changelog", TextInputStyle.Paragraph),
                CreateField("tool-website-url", "Website URL", isRequired: false,
                    placeholder: "https://www.look-at-my-mod.com"),
                CreateField("tool-download-url", "Download URL", isRequired: false,
                    placeholder: "https://www.download-my-mod-here.com"),
            });

        InteractionResponse data = new(InteractionCallbackType.Modal,
            new Optional<OneOf<IInteractionMessageCallbackData, IInteractionAutocompleteCallbackData,
                IInteractionModalCallbackData>>(modal));

        Result interactionResponseAsync = await _interactionApi.CreateInteractionResponseAsync(
            _interactionContext.Interaction.ID,
            _interactionContext.Interaction.Token,
            data);

        return interactionResponseAsync;
    }

    private static ActionRowComponent CreateField(string id, string label, TextInputStyle style = TextInputStyle.Short,
        bool isRequired = true, string placeholder = "")
    {
        return new ActionRowComponent(new[]
        {
            new TextInputComponent(
                id,
                style,
                label,
                default,
                default,
                isRequired,
                string.Empty,
                placeholder)
        });
    }
}
