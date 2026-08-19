using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Controllers;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;
using Xunit;

namespace ShopEasyMVC.Tests;

// ponytail: no ITempDataProvider registered in test host, so TempData needs a no-op provider.
file sealed class NullTempDataProvider : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}

public class OrdersControllerTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static OrdersController NewController(AppDbContext context)
    {
        var controller = new OrdersController(context);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new NullTempDataProvider());
        return controller;
    }

    private static async Task<User> SeedUserAsync(AppDbContext context)
    {
        var user = new User { FullName = "Cliente Test", Email = $"{Guid.NewGuid()}@test.com", PasswordHash = "x" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<Product> SeedProductAsync(AppDbContext context, string name, int stock)
    {
        var category = new Category { Name = $"Cat-{Guid.NewGuid()}" };
        var product = new Product { Name = name, CurrentPrice = 1000, Stock = stock, IsActive = true, Category = category };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }

    private static async Task<(Product product, Order order)> SeedCancelledOrderAsync(AppDbContext context, int stock, int orderedQty)
    {
        var user = await SeedUserAsync(context);
        var product = await SeedProductAsync(context, "Mouse", stock);

        var order = new Order
        {
            OrderNumber = "ORD-2026-001",
            Status = OrderStatus.Cancelled,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = orderedQty * 1000,
            User = user,
            OrderItems = new List<OrderItem> { new() { Product = product, Quantity = orderedQty, UnitPrice = 1000 } }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return (product, order);
    }

    [Fact]
    public async Task UpdateStatus_Reactivation_DecrementsStock_WhenAvailable()
    {
        using var context = NewContext();
        var (product, order) = await SeedCancelledOrderAsync(context, stock: 10, orderedQty: 3);
        var controller = NewController(context);

        await controller.UpdateStatus(order.Id, OrderStatus.Pending, null);

        var reloadedOrder = await context.Orders.FindAsync(order.Id);
        var reloadedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(OrderStatus.Pending, reloadedOrder!.Status);
        Assert.Equal(7, reloadedProduct!.Stock);
    }

    [Fact]
    public async Task UpdateStatus_Reactivation_RejectsAndKeepsStock_WhenInsufficient()
    {
        using var context = NewContext();
        var (product, order) = await SeedCancelledOrderAsync(context, stock: 2, orderedQty: 3);
        var controller = NewController(context);

        await controller.UpdateStatus(order.Id, OrderStatus.Pending, null);

        var reloadedOrder = await context.Orders.FindAsync(order.Id);
        var reloadedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(OrderStatus.Cancelled, reloadedOrder!.Status);
        Assert.Equal(2, reloadedProduct!.Stock);
    }

    [Fact]
    public async Task UpdateStatus_Cancellation_RestoresStock()
    {
        using var context = NewContext();
        var user = await SeedUserAsync(context);
        var product = await SeedProductAsync(context, "Teclado", stock: 5);
        var order = new Order
        {
            OrderNumber = "ORD-2026-002",
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = 5000,
            User = user,
            OrderItems = new List<OrderItem> { new() { Product = product, Quantity = 2, UnitPrice = 5000 } }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var controller = NewController(context);
        await controller.UpdateStatus(order.Id, OrderStatus.Cancelled, null);

        var reloadedProduct = await context.Products.FindAsync(product.Id);
        Assert.Equal(7, reloadedProduct!.Stock);
    }
}
