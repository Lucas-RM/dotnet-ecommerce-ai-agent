using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Microsoft.SemanticKernel;
using Refit;
using System.ComponentModel;

namespace ECommerce.AgentAPI.Infrastructure.Tools.Plugins;

public sealed class CartPlugin(IECommerceApi api)
{
    private readonly IECommerceApi _api = api;

    [Description("Retorna o carrinho atual do usuário com todos os itens, quantidades e total.")]
    [KernelFunction("get_cart")]
    public async Task<string> GetCartAsync()
    {
        var response = await _api.GetCartAsync();
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Adiciona um produto ao carrinho do usuário. SEMPRE requer confirmação explícita antes de executar. SEMPRE chame search_products antes para obter o productId correto.")]
    [KernelFunction("add_cart_item")]
    public async Task<string> AddCartItemAsync(
        [Description("ID do produto a ser adicionado (obtido via search_products)")] string productId,
        [Description("Quantidade a adicionar (default 1)")] int quantity = 1)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_api, productId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                new
                { success = false, message = "Não encontrei esse item na loja. Peça uma nova busca do produto e tente de novo." });
        }

        var dto = new AddCartItemDto(id.Value, quantity);
        var response = await _api.AddCartItemAsync(dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Atualiza a quantidade de um item já existente no carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("update_cart_item")]
    public async Task<string> UpdateCartItemAsync(
        [Description("UUID (campo 'id') do produto no retorno de search_products / carrinho")] string productId,
        [Description("Nova quantidade desejada")] int quantity)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_api, productId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                new
                { success = false, message = "Não encontrei esse item no carrinho. Confira o carrinho ou busque o produto de novo." });
        }

        var dto = new UpdateCartItemDto(quantity);
        var response = await _api.UpdateCartItemAsync(id.Value, dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Remove um produto específico do carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("remove_cart_item")]
    public async Task<string> RemoveCartItemAsync(
        [Description("UUID (campo 'id') do produto a remover")] string productId)
    {
        var id = await ProductIdResolver.TryResolveProductGuidAsync(_api, productId).ConfigureAwait(false);
        if (id is null)
        {
            return KernelJsonSerializer.Serialize(
                new
                { success = false, message = "Não encontrei esse item. Confira o carrinho ou busque o produto de novo." });
        }

        var response = await _api.RemoveCartItemAsync(id.Value);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description(
        "Esvazia completamente o carrinho do usuário. SEMPRE requer confirmação forte — ação remove todos os itens.")]
    [KernelFunction("clear_cart")]
    public async Task<string> ClearCartAsync()
    {
        IApiResponse response = await _api.ClearCartAsync();
        return KernelJsonSerializer.Serialize(new
        {
            success = response.IsSuccessStatusCode,
            statusCode = (int)response.StatusCode
        });
    }
}
