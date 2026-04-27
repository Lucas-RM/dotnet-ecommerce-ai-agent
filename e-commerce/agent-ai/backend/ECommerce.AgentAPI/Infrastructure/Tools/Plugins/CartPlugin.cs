using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Microsoft.SemanticKernel;
using Refit;
using System.ComponentModel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

[ToolPlugin]
public sealed class CartPlugin(ICartApi cartApi, IProductsApi productsApi)
{
    private readonly ICartApi _cartApi = cartApi;
    private readonly IProductsApi _productsApi = productsApi;

    [Description("Retorna o carrinho atual do usuário com todos os itens, quantidades e total.")]
    [KernelFunction("get_cart")]
    public async Task<string> GetCartAsync()
    {
        var response = await _cartApi.GetCartAsync();
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Adiciona um produto ao carrinho. Requer confirmação pelo sistema após a chamada. " +
        "Use o UUID (campo id) de search_products ou get_product. Não envie preço ou campos extra — o servidor resolve no catálogo.")]
    [KernelFunction("add_cart_item")]
    public async Task<string> AddCartItemAsync(
        [Description("UUID do produto (campo id em search_products / get_product).")] string productId,
        [Description("Quantidade a adicionar (default 1)")] int quantity = 1)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_productsApi, productId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                ToolPluginEnvelopeFactory.Failure(
                    "Não encontrei esse produto na loja com segurança. Peça para eu listar opções e informe o nome completo do produto."));
        }

        var dto = new AddCartItemDto(id.Value, quantity);
        var response = await _cartApi.AddCartItemAsync(dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Atualiza a quantidade de um item já existente no carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("update_cart_item")]
    public async Task<string> UpdateCartItemAsync(
        [Description("UUID (campo 'id') do produto no retorno de search_products / carrinho")] string productId,
        [Description("Nova quantidade desejada")] int quantity)
    {
        // Resolução estrita contra o carrinho: impede que um "2" (dígito vindo do LLM) vire "Produto 2"
        // via heurística de índice de catálogo. Se não der pra identificar o item dentro do carrinho atual,
        // devolvemos um envelope de erro para o LLM pedir get_cart e reconfirmar.
        var resolved = await ProductIdResolver.TryResolveCartItemAsync(_cartApi, productId).ConfigureAwait(false);
        if (resolved is null)
        {
            return KernelJsonSerializer.Serialize(
                ToolPluginEnvelopeFactory.Failure(
                    "Não consegui identificar esse item no carrinho com segurança. Peça para eu listar seu carrinho e informe o nome completo do produto (ex.: \"Produto Teste\")."));
        }

        var dto = new UpdateCartItemDto(quantity);
        var response = await _cartApi.UpdateCartItemAsync(resolved.Id, dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Remove um produto específico do carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("remove_cart_item")]
    public async Task<string> RemoveCartItemAsync(
        [Description("UUID (campo 'id') do produto a remover")] string productId)
    {
        var resolved = await ProductIdResolver.TryResolveCartItemAsync(_cartApi, productId).ConfigureAwait(false);
        if (resolved is null)
        {
            return KernelJsonSerializer.Serialize(
                ToolPluginEnvelopeFactory.Failure(
                    "Não consegui identificar esse item no carrinho com segurança. Peça para eu listar seu carrinho e informe o nome completo do produto."));
        }

        var response = await _cartApi.RemoveCartItemAsync(resolved.Id);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Esvazia completamente o carrinho do usuário. SEMPRE requer confirmação forte — ação remove todos os itens.")]
    [KernelFunction("clear_cart")]
    public async Task<string> ClearCartAsync()
    {
        IApiResponse response = await _cartApi.ClearCartAsync();
        if (!response.IsSuccessStatusCode)
        {
            return KernelJsonSerializer.Serialize(
                ToolPluginEnvelopeFactory.Failure(
                    "Não foi possível esvaziar o carrinho agora. Tente novamente em instantes."));
        }

        return KernelJsonSerializer.Serialize(
            ToolPluginEnvelopeFactory.Success(new ClearCartToolData((int)response.StatusCode)));
    }
}
