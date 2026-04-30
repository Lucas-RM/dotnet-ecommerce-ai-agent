using System.ComponentModel;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using ECommerce.AgentAPI.Infrastructure.Tools;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins.Parameters;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

[ToolPlugin]
public sealed class ProductPlugin(IProductsApi api)
{
    private readonly IProductsApi _api = api;

    [Description(
        "Busca ou lista produtos da loja. Com search e category vazios, lista a página de produtos (paginado). " +
        "Aumente pageSize (ex. 20) para ver mais itens numa lista ampla ou \"todos\" no sentido de uma página completa.")]
    [KernelFunction("search_products")]
    public async Task<string> SearchProductsAsync(SearchProductsParameters? parameters = null)
    {
        var input = parameters ?? new SearchProductsParameters();
        var effectivePage = input.Page <= 0 ? 1 : input.Page;
        var noTextFilter = string.IsNullOrWhiteSpace(input.Search) && string.IsNullOrWhiteSpace(input.Category);
        var defaultWideList = 20;
        var effectivePageSize = input.PageSize <= 0
            ? (noTextFilter ? defaultWideList : 5)
            : (noTextFilter && input.PageSize < defaultWideList ? defaultWideList : input.PageSize);
        var query = new ProductQueryParams(
            Page: effectivePage,
            PageSize: effectivePageSize,
            Category: string.IsNullOrEmpty(input.Category) ? null : input.Category,
            Search: string.IsNullOrEmpty(input.Search) ? null : input.Search);
        var response = await _api.GetProductsAsync(query);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Retorna detalhes completos de um produto pelo seu ID (Guid). Use após search_products para confirmar preço e estoque antes de adicionar ao carrinho.")]
    [KernelFunction("get_product")]
    public async Task<string> GetProductByIdAsync(GetProductParameters parameters)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_api, parameters.ProductId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                ToolPluginEnvelopeFactory.Failure(
                    "Não encontrei esse produto. Faça uma busca na loja e escolha um item da lista."));
        }

        var response = await _api.GetProductByIdAsync(id.Value);
        return KernelJsonSerializer.Serialize(response);
    }
}
