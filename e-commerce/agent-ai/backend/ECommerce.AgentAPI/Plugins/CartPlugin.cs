using System.ComponentModel;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Microsoft.SemanticKernel;
using Refit;

namespace ECommerce.AgentAPI.Plugins;

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
        if (!Guid.TryParse(productId, out var id))
            return KernelJsonSerializer.Serialize(new { success = false, message = "productId inválido ou ausente." });

        var dto = new AddCartItemDto(id, quantity);
        var response = await _api.AddCartItemAsync(dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Atualiza a quantidade de um item já existente no carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("update_cart_item")]
    public async Task<string> UpdateCartItemAsync(
        [Description("ID do produto cujo quantidade será alterada")] string productId,
        [Description("Nova quantidade desejada")] int quantity)
    {
        if (!Guid.TryParse(productId, out var id))
            return KernelJsonSerializer.Serialize(new { success = false, message = "productId inválido ou ausente." });

        var dto = new UpdateCartItemDto(quantity);
        var response = await _api.UpdateCartItemAsync(id, dto);
        return KernelJsonSerializer.Serialize(response);
    }

    [Description("Remove um produto específico do carrinho. SEMPRE requer confirmação.")]
    [KernelFunction("remove_cart_item")]
    public async Task<string> RemoveCartItemAsync(
        [Description("ID do produto a remover")] string productId)
    {
        if (!Guid.TryParse(productId, out var id))
            return KernelJsonSerializer.Serialize(new { success = false, message = "productId inválido ou ausente." });

        var response = await _api.RemoveCartItemAsync(id);
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
