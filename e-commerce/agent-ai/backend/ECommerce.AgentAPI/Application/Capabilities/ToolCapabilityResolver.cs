namespace ECommerce.AgentAPI.Application.Capabilities;

/// <summary>
/// Mapeia o nome de <c>[KernelFunction]</c> → capability usada em métricas e traces.
/// </summary>
public static class ToolCapabilityResolver
{
    public static AgentCapability Resolve(string? toolName)
    {
        if (string.IsNullOrEmpty(toolName))
            return AgentCapability.None;

        return toolName switch
        {
            "search_products" or "get_product" => AgentCapability.Catalog,
            "get_cart" or "add_cart_item" or "update_cart_item" or "remove_cart_item" or "clear_cart" =>
                AgentCapability.Cart,
            "list_orders" or "get_order" or "checkout" => AgentCapability.Orders,
            _ => AgentCapability.None
        };
    }
}
