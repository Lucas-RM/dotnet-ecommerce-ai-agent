using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Approval.Capabilities.Cart;

public sealed class CartItemApprovalEnrichmentStrategy : IToolApprovalArgumentEnrichmentStrategy
{
    private static readonly HashSet<string> SupportedTools =
    [
        "update_cart_item",
        "remove_cart_item"
    ];

    private readonly ICartApi _cartApi;

    public CartItemApprovalEnrichmentStrategy(ICartApi cartApi) =>
        _cartApi = cartApi;

    public bool CanHandle(string toolName) => SupportedTools.Contains(toolName ?? string.Empty);

    public async Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        var isRemove = string.Equals(toolName, "remove_cart_item", StringComparison.Ordinal);

        try
        {
            var raw = ApprovalArgumentEnrichmentSupport.ExtractProductIdentifier(arguments);
            var resolved = await ProductIdResolver
                .TryResolveCartItemAsync(_cartApi, raw, cancellationToken)
                .ConfigureAwait(false);
            if (resolved is null)
            {
                var action = isRemove ? "remover" : "atualizar";
                var error =
                    $"Não consegui identificar com segurança qual item você quer {action} no carrinho. Peça para eu listar seu carrinho e informe o nome completo do produto (ex.: *Produto X*).";
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
                ApprovalArgumentEnrichmentSupport.MapApiExceptionToBusinessMessage(apiEx, duringCartLookup: true));
        }
        catch (Exception)
        {
            const string generic =
                "Houve um problema ao consultar seu carrinho agora. Tente novamente em instantes.";
            return new ApprovalArgumentEnrichment(arguments, generic);
        }
    }
}
