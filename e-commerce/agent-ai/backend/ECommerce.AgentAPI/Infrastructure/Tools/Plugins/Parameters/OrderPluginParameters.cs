using System.ComponentModel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins.Parameters;

public sealed class ListOrdersParameters
{
    [Description("Página da listagem (default 1)")]
    public int Page { get; init; } = 1;

    [Description("Quantidade por página (default 5)")]
    public int PageSize { get; init; } = 5;
}

public sealed class GetOrderParameters
{
    [Description("Número do pedido (UUID da coluna Pedido), prefixo desse número, data/hora da listagem, ou \"último pedido\".")]
    public string OrderId { get; init; } = string.Empty;
}
