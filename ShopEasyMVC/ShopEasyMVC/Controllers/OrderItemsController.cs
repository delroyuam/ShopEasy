using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Controllers
{
    [Authorize(Roles = "admin")]
    public class OrderItemsController : Controller
    {
        private readonly AppDbContext _context;

        public OrderItemsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .ToListAsync();

            return View(orderItems);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem is null)
            {
                return NotFound();
            }

            return View(orderItem);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelectListsAsync();
            return View(new OrderItem { Quantity = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Quantity,UnitPrice,OrderId,ProductId")] OrderItem orderItem)
        {
            ModelState.Remove("Order");
            ModelState.Remove("Product");

            await ValidateOrderItemAsync(orderItem);

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(orderItem.ProductId);
                if (product is not null)
                {
                    product.Stock -= orderItem.Quantity;
                }

                _context.OrderItems.Add(orderItem);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadSelectListsAsync(orderItem.OrderId, orderItem.ProductId);
            return View(orderItem);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems.FindAsync(id);

            if (orderItem is null)
            {
                return NotFound();
            }

            await LoadSelectListsAsync(orderItem.OrderId, orderItem.ProductId);
            return View(orderItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Quantity,UnitPrice,OrderId,ProductId")] OrderItem orderItem)
        {
            if (id != orderItem.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Order");
            ModelState.Remove("Product");

            var existingOrderItem = await _context.OrderItems.FindAsync(id);

            if (existingOrderItem is null)
            {
                return NotFound();
            }

            await ValidateOrderItemAsync(orderItem, existingOrderItem);

            if (ModelState.IsValid)
            {
                try
                {
                    var previousProduct = await _context.Products.FindAsync(existingOrderItem.ProductId);
                    if (previousProduct is not null)
                    {
                        previousProduct.Stock += existingOrderItem.Quantity;
                    }

                    var newProduct = await _context.Products.FindAsync(orderItem.ProductId);
                    if (newProduct is not null)
                    {
                        newProduct.Stock -= orderItem.Quantity;
                    }

                    existingOrderItem.Quantity = orderItem.Quantity;
                    existingOrderItem.UnitPrice = orderItem.UnitPrice;
                    existingOrderItem.OrderId = orderItem.OrderId;
                    existingOrderItem.ProductId = orderItem.ProductId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderItemExists(orderItem.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadSelectListsAsync(orderItem.OrderId, orderItem.ProductId);
            return View(orderItem);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem is null)
            {
                return NotFound();
            }

            return View(orderItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem is not null)
            {
                if (orderItem.Order is null || orderItem.Order.Status != OrderStatus.Cancelled)
                {
                    var product = await _context.Products.FindAsync(orderItem.ProductId);
                    if (product is not null)
                    {
                        product.Stock += orderItem.Quantity;
                    }
                }

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderItemExists(int id)
        {
            return _context.OrderItems.Any(oi => oi.Id == id);
        }

        private async Task ValidateOrderItemAsync(OrderItem orderItem, OrderItem? existingOrderItem = null)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderItem.OrderId);

            if (order is null)
            {
                ModelState.AddModelError("OrderId", "Debe seleccionar una orden válida.");
            }
            else if (order.Status != OrderStatus.Pending)
            {
                ModelState.AddModelError("OrderId", "Solo se pueden agregar productos a órdenes pendientes.");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == orderItem.ProductId);

            if (product is null)
            {
                ModelState.AddModelError("ProductId", "Debe seleccionar un producto válido.");
            }
            else
            {
                var alreadyReserved = existingOrderItem is not null && existingOrderItem.ProductId == product.Id
                    ? existingOrderItem.Quantity
                    : 0;
                var available = product.Stock + alreadyReserved;

                if (available <= 0)
                {
                    ModelState.AddModelError("ProductId", "El producto está agotado.");
                }
                else if (orderItem.Quantity > available)
                {
                    ModelState.AddModelError("Quantity", $"Solo hay {available} unidad(es) disponible(s).");
                }
            }

            var duplicatedProductInOrder = await _context.OrderItems.AnyAsync(oi =>
                oi.OrderId == orderItem.OrderId
                && oi.ProductId == orderItem.ProductId
                && (existingOrderItem == null || oi.Id != existingOrderItem.Id));

            if (duplicatedProductInOrder)
            {
                ModelState.AddModelError("ProductId", "Este producto ya existe en esa orden.");
            }
        }

        private async Task LoadSelectListsAsync(int? selectedOrderId = null, int? selectedProductId = null)
        {
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderBy(o => o.OrderNumber)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.IsActive && (p.Stock > 0 || p.Id == selectedProductId))
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewData["OrderId"] = new SelectList(orders, "Id", "OrderNumber", selectedOrderId);
            ViewData["ProductId"] = new SelectList(products, "Id", "Name", selectedProductId);

            ViewData["ProductPrices"] = JsonSerializer.Serialize(
                products.ToDictionary(product => product.Id, product => product.CurrentPrice));
        }
    }
}
