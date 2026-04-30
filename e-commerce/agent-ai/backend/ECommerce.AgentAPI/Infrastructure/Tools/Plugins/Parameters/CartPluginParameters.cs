using System.ComponentModel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins.Parameters;

public sealed class AddCartItemParameters
{
    [Description("UUID do produto (campo id em search_products / get_product).")]
    public string ProductId { get; init; } = string.Empty;

    [Description("Quantidade a adicionar (default 1)")]
    public int Quantity { get; init; } = 1;
}

public sealed class UpdateCartItemParameters
{
    [Description("UUID (campo 'id') do produto no retorno de search_products / carrinho")]
    public string ProductId { get; init; } = string.Empty;

    [Description("Nova quantidade desejada")]
    public int Quantity { get; init; }
}

public sealed class RemoveCartItemParameters
{
    [Description("UUID (campo 'id') do produto a remover")]
    public string ProductId { get; init; } = string.Empty;
}
