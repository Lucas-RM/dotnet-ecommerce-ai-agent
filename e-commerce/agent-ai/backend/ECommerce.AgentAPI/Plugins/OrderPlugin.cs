using System.ComponentModel;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Plugins;

public sealed class OrderPlugin(IECommerceApi api)
{
    private readonly IECommerceApi _api = api;

    [Description("Lista os pedidos realizados pelo usuário com status e valor total.")]
    [KernelFunction("list_orders")]
    public async Task<string> ListOrdersAsync(
        [Description("Página da listagem (default 1)")] int? page = null,
        [Description("Quantidade por página (default 5)")] int? pageSize = null)
    {
        var query = new OrderQueryParams(Page: page ?? 1, PageSize: pageSize ?? 5);
        var response = await _api.GetOrdersAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Retorna os detalhes de um pedido específico, incluindo itens e status atual.")]
    [KernelFunction("get_order")]
    public async Task<string> GetOrderByIdAsync(
        [Description("ID do pedido (obtido via list_orders)")] string orderId)
    {
        if (!Guid.TryParse(orderId, out var id))
            return KernelJsonSerializer.Serialize(new { success = false, message = "orderId inválido ou ausente." });

        var response = await _api.GetOrderByIdAsync(id);
        return KernelJsonSerializer.Serialize(response);
    }
}
