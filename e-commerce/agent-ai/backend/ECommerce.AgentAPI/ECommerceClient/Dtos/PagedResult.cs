namespace ECommerce.AgentAPI.ECommerceClient.Dtos;

/// <summary>Espelha <c>ECommerce.Application.Common.PagedResult&lt;T&gt;</c>.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
