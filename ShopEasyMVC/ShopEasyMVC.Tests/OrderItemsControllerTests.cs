using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Controllers;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;
using Xunit;

namespace ShopEasyMVC.Tests;

public class OrderItemsControllerTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Product product, Order order, OrderItem item)> SeedOrderWithItemAsync(
        AppDbContext context, OrderStatus orderStatus, int stock, int quantity)
    {
        var user = new User { FullName = "Cliente Test", Email = $"{Guid.NewGuid()}@test.com", PasswordHash = "x" };
        var category = new Category { Name = $"Cat-{Guid.NewGuid()}" };
        var product = new Product { Name = "Monitor", CurrentPrice = 2000, Stock = stock, IsActive = true, Category = category };
        var order = new Order
        {
            OrderNumber = $"ORD-2026-{Guid.NewGuid():N}"[..14],
            Status = orderStatus,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = quantity * 2000,
            User = user
        };
        var item = new OrderItem { Product = product, Order = order, Quantity = quantity, UnitPrice = 2000 };

        context.AddRange(user, product, order, item);
        await context.SaveChangesAsync();
        return (product, order, item);
    }

    [Fact]
    public async Task Delete_OnPendingOrder_RemovesItemAndRestoresStock()
    {
        using var context = NewContext();
        var (product, _, item) = await SeedOrderWithItemAsync(context, OrderStatus.Pending, stock: 5, quantity: 2);
        var controller = new OrderItemsController(context);

        await controller.DeleteConfirmed(item.Id);

        Assert.Null(await context.OrderItems.FindAsync(item.Id));
        var reloadedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(7, reloadedProduct!.Stock);
    }

    [Fact]
    public async Task Delete_OnShippedOrder_IsBlocked_AndStockUnchanged()
    {
        using var context = NewContext();
        var (product, _, item) = await SeedOrderWithItemAsync(context, OrderStatus.Shipped, stock: 5, quantity: 2);
        var controller = new OrderItemsController(context);

        await controller.DeleteConfirmed(item.Id);

        Assert.NotNull(await context.OrderItems.FindAsync(item.Id));
        var reloadedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(5, reloadedProduct!.Stock);
    }
}
