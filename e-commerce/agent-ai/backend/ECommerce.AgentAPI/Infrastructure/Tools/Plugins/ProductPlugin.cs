using System.ComponentModel;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

public sealed class ProductPlugin(IECommerceApi api)
{
    private readonly IECommerceApi _api = api;

    [Description(
        "Busca ou lista produtos da loja. Com search e category vazios, lista a página de produtos (paginado). " +
        "Aumente pageSize (ex. 20) para ver mais itens numa lista ampla ou \"todos\" no sentido de uma página completa.")]
    [KernelFunction("search_products")]
    public async Task<string> SearchProductsAsync(
        [Description("Termo de busca (nome do produto). Vazio = sem filtro de texto, útil para listar a loja.")] string search = "",
        [Description("Categoria do produto para filtrar (ex: Eletrônicos). Vazio = todas as categorias.")] string category = "",
        [Description("Página dos resultados (default 1)")] int page = 1,
        [Description("Quantidade de resultados por página. Default 5; para listar muitos itens, use 20–50.")] int pageSize = 5)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var noTextFilter = string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(category);
        var defaultWideList = 20;
        var effectivePageSize = pageSize <= 0
            ? (noTextFilter ? defaultWideList : 5)
            : (noTextFilter && pageSize < defaultWideList ? defaultWideList : pageSize);
        var query = new ProductQueryParams(
            Page: effectivePage,
            PageSize: effectivePageSize,
            Category: string.IsNullOrEmpty(category) ? null : category,
            Search: string.IsNullOrEmpty(search) ? null : search);
        var response = await _api.GetProductsAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Retorna detalhes completos de um produto pelo seu ID (Guid). Use após search_products para confirmar preço e estoque antes de adicionar ao carrinho.")]
    [KernelFunction("get_product")]
    public async Task<string> GetProductByIdAsync(
        [Description("ID único do produto retornado pelo search_products")] string productId)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_api, productId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                new { success = false, message = "Não encontrei esse produto. Faça uma busca na loja e escolha um item da lista." });
        }

        var response = await _api.GetProductByIdAsync(id.Value);
        return KernelJsonSerializer.Serialize(response);
    }
}
