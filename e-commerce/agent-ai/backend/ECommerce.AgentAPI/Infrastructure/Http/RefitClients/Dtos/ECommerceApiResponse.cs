namespace ECommerce.AgentAPI.ECommerceClient.Dtos;

/// <summary>
/// Envelope JSON da API e-commerce (espelha <c>ECommerce.Application.Common.ApiResponse&lt;T&gt;</c>).
/// Nome distinto de <c>Refit.ApiResponse&lt;T&gt;</c>.
/// </summary>
public sealed class ECommerceApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
}
