using System.ComponentModel;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Plugins;

public sealed class ProductPlugin(IECommerceApi api)
{
    private readonly IECommerceApi _api = api;

    [Description(
        "Busca produtos na loja pelo nome ou categoria. Use quando o usuário quiser encontrar um produto antes de adicioná-lo ao carrinho.")]
    [KernelFunction("search_products")]
    public async Task<string> SearchProductsAsync(
        [Description("Termo de busca (nome do produto ou palavra-chave)")] string? search = null,
        [Description("Categoria do produto para filtrar (ex: Eletrônicos)")] string? category = null,
        [Description("Página dos resultados (default 1)")] int? page = null,
        [Description("Quantidade de resultados por página (default 5)")] int? pageSize = null)
    {
        var query = new ProductQueryParams(
            Page: page ?? 1,
            PageSize: pageSize ?? 5,
            Category: category,
            Search: search);
        var response = await _api.GetProductsAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Retorna detalhes completos de um produto pelo seu ID (Guid). Use após search_products para confirmar preço e estoque antes de adicionar ao carrinho.")]
    [KernelFunction("get_product")]
    public async Task<string> GetProductByIdAsync(
        [Description("ID único do produto retornado pelo search_products")] string productId)
    {
        if (!Guid.TryParse(productId, out var id))
            return KernelJsonSerializer.Serialize(new { success = false, message = "productId inválido ou ausente." });

        var response = await _api.GetProductByIdAsync(id);
        return KernelJsonSerializer.Serialize(response);
    }
}
