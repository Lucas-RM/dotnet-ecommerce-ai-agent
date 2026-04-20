namespace ECommerce.Domain.Entities;

public sealed class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = [];
}
