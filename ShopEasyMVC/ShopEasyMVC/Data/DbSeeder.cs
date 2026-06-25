using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Users.AnyAsync() || await context.Categories.AnyAsync())
            {
                return;
            }

            context.UserRoles.AddRange(
                new UserRole { Name = "admin" },
                new UserRole { Name = "cliente" });

            var passwordHasher = new PasswordHasher<User>();

            var admin = new User
            {
                FullName = "Admin ShopEasy",
                Email = "admin@shopeasy.com"
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");
            admin.UserRoles.Add(new UserRole { Name = "admin" });

            var cliente = new User
            {
                FullName = "Cliente Demo",
                Email = "cliente@shopeasy.com"
            };
            cliente.PasswordHash = passwordHasher.HashPassword(cliente, "Cliente123!");
            cliente.UserRoles.Add(new UserRole { Name = "cliente" });

            context.Users.AddRange(admin, cliente);

            var electronica = new Category { Name = "Electrónica", Description = "Dispositivos, gadgets y accesorios." };
            var hogar = new Category { Name = "Hogar", Description = "Artículos para el hogar y la cocina." };
            var ropa = new Category { Name = "Ropa", Description = "Moda y vestuario para toda ocasión." };
            var libros = new Category { Name = "Libros", Description = "Lectura, estudio y conocimiento." };

            context.Categories.AddRange(electronica, hogar, ropa, libros);

            var laptop = new Product { Name = "Laptop Pro 15", Description = "Portátil de 15 pulgadas con 16GB RAM y SSD de 512GB.", CurrentPrice = 695000m, Stock = 8, Category = electronica, ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 1, 5) };
            var audifonos = new Product { Name = "Audífonos Bluetooth", Description = "Audífonos inalámbricos con cancelación de ruido.", CurrentPrice = 42500m, Stock = 3, Category = electronica, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 2, 14) };
            var smartphone = new Product { Name = "Smartphone X", Description = "Teléfono de gama alta con cámara de 108MP.", CurrentPrice = 479000m, Stock = 0, Category = electronica, ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 3, 22) };
            var monitor = new Product { Name = "Monitor 27 4K", Description = "Monitor 4K UHD de 27 pulgadas.", CurrentPrice = 189000m, Stock = 15, Category = electronica, ImageUrl = "https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 5, 9) };
            var cafetera = new Product { Name = "Cafetera Express", Description = "Cafetera espresso automática de 15 bares.", CurrentPrice = 69500m, Stock = 12, Category = hogar, ImageUrl = "https://images.unsplash.com/photo-1453614512568-c4024d13c247?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 4, 1) };
            var sabanas = new Product { Name = "Juego de Sábanas", Description = "Juego de sábanas de algodón 100%, tamaño queen.", CurrentPrice = 26500m, Stock = 25, Category = hogar, ImageUrl = "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 6, 18) };
            var lampara = new Product { Name = "Lámpara LED", Description = "Lámpara de escritorio LED con brillo regulable.", CurrentPrice = 13500m, Stock = 2, Category = hogar, ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 7, 30) };
            var camiseta = new Product { Name = "Camiseta Básica", Description = "Camiseta de algodón unisex, varios colores.", CurrentPrice = 7950m, Stock = 50, Category = ropa, ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 8, 12) };
            var chaqueta = new Product { Name = "Chaqueta de Cuero", Description = "Chaqueta de cuero sintético estilo motero.", CurrentPrice = 106000m, Stock = 6, Category = ropa, ImageUrl = "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 9, 25) };
            var novela = new Product { Name = "Novela Bestseller", Description = "Novela de misterio más vendida del año.", CurrentPrice = 10900m, Stock = 30, Category = libros, ImageUrl = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 10, 3) };
            var guiaProg = new Product { Name = "Guía de Programación", Description = "Guía completa de C# y ASP.NET Core.", CurrentPrice = 21500m, Stock = 0, Category = libros, ImageUrl = "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=600&h=600&fit=crop", CreatedAtUtc = new DateTime(2025, 11, 17) };

            context.Products.AddRange(laptop, audifonos, smartphone, monitor, cafetera, sabanas, lampara, camiseta, chaqueta, novela, guiaProg);

            var orders = new List<Order>
            {
                BuildOrder("ORD-2025-001", new DateTime(2025, 1, 20), OrderStatus.Delivered, cliente,
                    (laptop, 1), (audifonos, 2)),
                BuildOrder("ORD-2025-002", new DateTime(2025, 4, 8), OrderStatus.Delivered, cliente,
                    (cafetera, 1), (sabanas, 2)),
                BuildOrder("ORD-2025-003", new DateTime(2025, 8, 15), OrderStatus.Shipped, admin,
                    (monitor, 2), (camiseta, 3)),
                BuildOrder("ORD-2025-004", new DateTime(2025, 11, 2), OrderStatus.Cancelled, cliente,
                    (chaqueta, 1)),
                BuildOrder("ORD-2026-001", new DateTime(2026, 2, 11), OrderStatus.Pending, cliente,
                    (novela, 1), (lampara, 2)),
                BuildOrder("ORD-2026-002", new DateTime(2026, 6, 18), OrderStatus.Pending, admin,
                    (camiseta, 5), (sabanas, 1), (novela, 2))
            };

            context.Orders.AddRange(orders);

            foreach (var order in orders.Where(order => order.Status != OrderStatus.Cancelled))
            {
                foreach (var item in order.OrderItems)
                {
                    item.Product.Stock -= item.Quantity;
                }
            }

            await context.SaveChangesAsync();
        }

        private static Order BuildOrder(string orderNumber, DateTime createdAt, OrderStatus status, User user,
            params (Product product, int quantity)[] lines)
        {
            var order = new Order
            {
                OrderNumber = orderNumber,
                CreatedAt = createdAt,
                Status = status,
                User = user,
                OrderItems = lines
                    .Select(line => new OrderItem
                    {
                        Product = line.product,
                        Quantity = line.quantity,
                        UnitPrice = line.product.CurrentPrice
                    })
                    .ToList()
            };

            order.TotalAmount = order.OrderItems.Sum(item => item.Quantity * item.UnitPrice);

            return order;
        }
    }
}
