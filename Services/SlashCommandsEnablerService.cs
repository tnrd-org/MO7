using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace MO7.Services;

public class SlashCommandsEnablerService : BackgroundService
{
    private readonly ILogger<SlashCommandsEnablerService> _logger;
    private readonly SlashService _slashService;

    public SlashCommandsEnablerService(ILogger<SlashCommandsEnablerService> logger, SlashService slashService)
    {
        _logger = logger;
        _slashService = slashService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Result result = await _slashService.UpdateSlashCommandsAsync(ct: stoppingToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Slash commands updated successfully");
        }
        else
        {
            _logger.LogError("Failed to update slash commands: {Error}", result.Error);
        }
    }
}
