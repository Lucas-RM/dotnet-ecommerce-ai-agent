using System.ComponentModel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins.Parameters;

public sealed class SearchProductsParameters
{
    [Description("Termo de busca (nome do produto). Vazio = sem filtro de texto, útil para listar a loja.")]
    public string Search { get; init; } = string.Empty;

    [Description("Categoria do produto para filtrar (ex: Eletrônicos). Vazio = todas as categorias.")]
    public string Category { get; init; } = string.Empty;

    [Description("Página dos resultados (default 1)")]
    public int Page { get; init; } = 1;

    [Description("Quantidade de resultados por página. Default 5; para listar muitos itens, use 20–50.")]
    public int PageSize { get; init; } = 5;
}

public sealed class GetProductParameters
{
    [Description("ID único do produto retornado pelo search_products")]
    public string ProductId { get; init; } = string.Empty;
}
