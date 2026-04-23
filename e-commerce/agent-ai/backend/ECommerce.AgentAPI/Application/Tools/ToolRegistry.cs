using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

/// <summary> Definições de tools (nome, descrição, parâmetros) para o LLM — alinhado aos plugins. </summary>
public static class ToolRegistry
{
    public static IReadOnlyList<ToolDefinition> GetDefinitions() =>
    [
        SearchProducts(),
        GetProduct(),
        GetCart(),
        AddCartItem(),
        UpdateCartItem(),
        RemoveCartItem(),
        ClearCart(),
        ListOrders(),
        GetOrder(),
        Checkout()
    ];

    private static ToolDefinition SearchProducts() => new()
    {
        Name = "search_products",
        Description = "Busca ou lista produtos. search/category vazios = listar a página; aumente pageSize (ex. 20) para mais itens.",
        Parameters =
        [
            new() { Name = "search", Type = "string", Description = "Termo de busca" },
            new() { Name = "category", Type = "string", Description = "Categoria", Required = false },
            new() { Name = "page", Type = "integer", Description = "Página (default 1)", Required = false },
            new() { Name = "pageSize", Type = "integer", Description = "Itens por página (default 5)", Required = false }
        ]
    };

    private static ToolDefinition GetProduct() => new()
    {
        Name = "get_product",
        Description = "Detalhes de um produto pelo id (Guid) exatamente como em search_products.",
        Parameters = [
            new()
            {
                Name = "productId",
                Type = "string",
                Description = "UUID do produto: campo 'id' no JSON de search_products (não o número do nome).",
                Required = true
            }
        ]
    };

    private static ToolDefinition GetCart() => new()
    {
        Name = "get_cart",
        Description = "Carrinho atual com itens e totais.",
        Parameters = []
    };

    private static ToolDefinition AddCartItem() => new()
    {
        Name = "add_cart_item",
        Description = "Adiciona um produto ao carrinho (sempre após confirmação). Só após search_products; productId = campo 'id' (UUID) do retorno.",
        Parameters =
        [
            new()
            {
                Name = "productId",
                Type = "string",
                Description = "UUID do retorno de search_products (campo 'id'). Opcional: extras productName, unitPrice para a mensagem de confirmação.",
                Required = true
            },
            new() { Name = "quantity", Type = "integer", Description = "Quantidade", Required = true }
        ]
    };

    private static ToolDefinition UpdateCartItem() => new()
    {
        Name = "update_cart_item",
        Description = "Atualiza quantidade de um item no carrinho. productId = UUID do produto (campo 'id' de search_products).",
        Parameters =
        [
            new()
            {
                Name = "productId",
                Type = "string",
                Description = "UUID (campo 'id') do produto; nunca inventar ou trocar por número do título 'Produto N'.",
                Required = true
            },
            new() { Name = "quantity", Type = "integer", Description = "Nova quantidade", Required = true }
        ]
    };

    private static ToolDefinition RemoveCartItem() => new()
    {
        Name = "remove_cart_item",
        Description = "Remove um produto do carrinho. productId = UUID do retorno de search_products.",
        Parameters = [
            new()
            {
                Name = "productId",
                Type = "string",
                Description = "UUID (campo 'id') do produto.",
                Required = true
            }
        ]
    };

    private static ToolDefinition ClearCart() => new()
    {
        Name = "clear_cart",
        Description = "Esvazia o carrinho (ação irreversível).",
        Parameters = []
    };

    private static ToolDefinition ListOrders() => new()
    {
        Name = "list_orders",
        Description = "Lista pedidos do usuário.",
        Parameters =
        [
            new() { Name = "page", Type = "integer", Description = "Página", Required = false },
            new() { Name = "pageSize", Type = "integer", Description = "Tamanho da página", Required = false }
        ]
    };

    private static ToolDefinition GetOrder() => new()
    {
        Name = "get_order",
        Description = "Detalhes de um pedido pelo ID.",
        Parameters = [new() { Name = "orderId", Type = "string", Description = "ID do pedido", Required = true }]
    };

    private static ToolDefinition Checkout() => new()
    {
        Name = "checkout",
        Description = "Finaliza o pedido a partir do carrinho (sempre após confirmação).",
        Parameters = []
    };
}
