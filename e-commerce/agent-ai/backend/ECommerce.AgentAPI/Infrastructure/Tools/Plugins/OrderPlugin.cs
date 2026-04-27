using System.ComponentModel;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

[ToolPlugin]
public sealed class OrderPlugin(IOrdersApi ordersApi, ICheckoutApi checkoutApi)
{
    private readonly IOrdersApi _ordersApi = ordersApi;
    private readonly ICheckoutApi _checkoutApi = checkoutApi;

    [Description("Lista os pedidos realizados pelo usuário com status e valor total.")]
    [KernelFunction("list_orders")]
    public async Task<string> ListOrdersAsync(
        [Description("Página da listagem (default 1)")] int page = 1,
        [Description("Quantidade por página (default 5)")] int pageSize = 5)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var effectivePageSize = pageSize <= 0 ? 5 : pageSize;
        var query = new OrderQueryParams(Page: effectivePage, PageSize: effectivePageSize);
        var response = await _ordersApi.GetOrdersAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Retorna os detalhes de um pedido específico, incluindo itens e status atual. " +
        "O utilizador vê o identificador na coluna Pedido da listagem; aceite esse texto completo, " +
        "prefixo hexadecimal (8+ caracteres), data/hora alinhada à listagem, ou \"último pedido\" / referência vaga " +
        "quando só existir um pedido.")]
    [KernelFunction("get_order")]
    public async Task<string> GetOrderByIdAsync(
        [Description(
            "Número do pedido (UUID da coluna Pedido), prefixo desse número, data/hora da listagem, ou \"último pedido\".")] string orderId,
        CancellationToken cancellationToken = default)
    {
        var id = await OrderIdResolver
            .TryResolveOrderGuidAsync(_ordersApi, orderId, cancellationToken)
            .ConfigureAwait(false);
        if (id is null)
        {
            const string msg =
                "Não consegui identificar esse pedido. Copie o **número completo** da coluna **Pedido** na sua lista e envie de novo, "
                + "ou tente a **data e hora** no mesmo formato da tabela, por exemplo **18/04/2026 20:18** (dia/mês/ano e hora) "
                + "ou **4/18/2026 8:18 PM** (mês/dia/ano em estilo americano). "
                + "Se for o pedido mais recente, pode escrever **último pedido**. "
                + "Se houver vários pedidos parecidos, envie o número do pedido para não haver dúvida.";
            return KernelJsonSerializer.Serialize(ToolPluginEnvelopeFactory.Failure(msg));
        }

        var response = await _ordersApi.GetOrderByIdAsync(id.Value).ConfigureAwait(false);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Finaliza a compra: cria um pedido a partir do carrinho atual (checkout). SEMPRE requer confirmação explícita antes de executar. " +
        "Só chame após o usuário ter itens no carrinho e confirmar que deseja concluir a compra.")]
    [KernelFunction("checkout")]
    public async Task<string> CheckoutAsync()
    {
        var response = await _checkoutApi.CheckoutAsync();
        return KernelJsonSerializer.Serialize(response);
    }
}
