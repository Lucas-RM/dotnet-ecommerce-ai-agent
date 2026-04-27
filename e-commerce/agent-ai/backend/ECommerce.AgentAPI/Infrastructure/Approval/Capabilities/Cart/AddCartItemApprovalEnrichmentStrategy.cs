using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Approval.Capabilities.Cart;

public sealed class AddCartItemApprovalEnrichmentStrategy : IToolApprovalArgumentEnrichmentStrategy
{
    private readonly IProductsApi _productsApi;

    public AddCartItemApprovalEnrichmentStrategy(IProductsApi productsApi) =>
        _productsApi = productsApi;

    public bool CanHandle(string toolName) =>
        string.Equals(toolName, "add_cart_item", StringComparison.Ordinal);

    public async Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = ApprovalArgumentEnrichmentSupport.ExtractProductIdentifier(arguments);
            var resolved = await ProductIdResolver
                .TryResolveCatalogProductAsync(_productsApi, raw, cancellationToken)
                .ConfigureAwait(false);
            if (resolved is null)
            {
                const string error =
                    "Não localizei esse produto na loja com segurança. Peça para eu listar opções e informe o nome completo do produto.";
                return new ApprovalArgumentEnrichment(arguments, error);
            }

            return new ApprovalArgumentEnrichment(
                ApprovalArgumentEnrichmentSupport.EnrichWithResolvedProduct(arguments, resolved),
                null);
        }
        catch (ApiException apiEx)
        {
            return new ApprovalArgumentEnrichment(
                arguments,
                ApprovalArgumentEnrichmentSupport.MapApiExceptionToBusinessMessage(apiEx, duringCartLookup: false));
        }
        catch (Exception)
        {
            const string generic =
                "Houve um problema ao consultar os produtos da loja agora. Tente novamente em instantes.";
            return new ApprovalArgumentEnrichment(arguments, generic);
        }
    }
}
