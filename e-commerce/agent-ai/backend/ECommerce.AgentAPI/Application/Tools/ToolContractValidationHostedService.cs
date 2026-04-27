namespace ECommerce.AgentAPI.Application.Tools;

public sealed class ToolContractValidationHostedService : IHostedService
{
    private readonly ToolCatalog _catalog;

    public ToolContractValidationHostedService(ToolCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var contracts = ToolContractComposer.Compose(_catalog);
        ToolContractValidator.ValidateOrThrow(_catalog, contracts);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
