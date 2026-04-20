using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Infrastructure.Data;

public sealed class DataSeeder(AppDbContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Name = "Admin", Email = "admin@ecommerce.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456", 12), Role = Role.Admin },
                new User { Name = "Customer", Email = "user@ecommerce.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123456", 12), Role = Role.Customer }
            );
        }

        if (!db.Products.Any())
        {
            var products = Enumerable.Range(1, 10).Select(i => new Product
            {
                Name = $"Produto {i}",
                Description = $"Descrição do produto {i}",
                Price = 10 + i,
                StockQuantity = 25 + i,
                Category = i % 3 == 0 ? "Eletrônicos" : i % 2 == 0 ? "Casa" : "Moda"
            });
            db.Products.AddRange(products);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
