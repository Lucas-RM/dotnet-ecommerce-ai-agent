using System.ComponentModel;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

public sealed class OrderPlugin(IECommerceApi api)
{
    private readonly IECommerceApi _api = api;

    [Description("Lista os pedidos realizados pelo usuário com status e valor total.")]
    [KernelFunction("list_orders")]
    public async Task<string> ListOrdersAsync(
        [Description("Página da listagem (default 1)")] int page = 1,
        [Description("Quantidade por página (default 5)")] int pageSize = 5)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var effectivePageSize = pageSize <= 0 ? 5 : pageSize;
        var query = new OrderQueryParams(Page: effectivePage, PageSize: effectivePageSize);
        var response = await _api.GetOrdersAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Retorna os detalhes de um pedido específico, incluindo itens e status atual.")]
    [KernelFunction("get_order")]
    public async Task<string> GetOrderByIdAsync(
        [Description("ID do pedido (obtido via list_orders)")] string orderId)
    {
        if (!Guid.TryParse(orderId, out var id))
            return KernelJsonSerializer.Serialize(
                new { success = false, message = "Não consegui abrir esse pedido. Abra a lista de pedidos e escolha um válido." });

        var response = await _api.GetOrderByIdAsync(id);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Finaliza a compra: cria um pedido a partir do carrinho atual (checkout). SEMPRE requer confirmação explícita antes de executar. " +
        "Só chame após o usuário ter itens no carrinho e confirmar que deseja concluir a compra.")]
    [KernelFunction("checkout")]
    public async Task<string> CheckoutAsync()
    {
        var response = await _api.CheckoutAsync();
        return KernelJsonSerializer.Serialize(response);
    }
}
