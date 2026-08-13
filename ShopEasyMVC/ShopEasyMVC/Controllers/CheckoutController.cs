using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Helpers;
using ShopEasyMVC.Models;
using ShopEasyMVC.Services;

namespace ShopEasyMVC.Controllers
    {
    [Authorize]
    public class CheckoutController : Controller
        {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public CheckoutController(AppDbContext context, IEmailSender emailSender)
            {
            _context = context;
            _emailSender = emailSender;
            }

        public IActionResult Index()
            {
            var cart = HttpContext.Session.GetCart();

            if (cart.Count == 0)
                {
                return RedirectToAction("Index", "Cart");
                }

            return View(cart);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(string shippingAddress, PaymentMethod paymentMethod)
            {
            var cart = HttpContext.Session.GetCart();

            if (cart.Count == 0)
                {
                return RedirectToAction("Index", "Cart");
                }

            shippingAddress = (shippingAddress ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(shippingAddress))
                {
                ModelState.AddModelError(string.Empty, "La dirección de envío es obligatoria.");
                return View("Index", cart);
                }

            var products = await _context.Products
                .Where(p => cart.Select(l => l.ProductId).Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var line in cart)
                {
                if (!products.TryGetValue(line.ProductId, out var product) || product.Stock < line.Quantity)
                    {
                    ModelState.AddModelError(string.Empty, $"No hay suficiente stock de {line.ProductName}.");
                    return View("Index", cart);
                    }
                }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var order = new Order
                {
                OrderNumber = await OrderNumberGenerator.GenerateAsync(_context, DateTime.UtcNow.Year),
                UserId = userId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ShippingAddress = shippingAddress,
                PaymentMethod = paymentMethod,
                OrderItems = cart.Select(line => new OrderItem
                    {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                    }).ToList()
                };

            order.TotalAmount = cart.Sum(l => l.Subtotal);

            foreach (var line in cart)
                {
                products[line.ProductId].Stock -= line.Quantity;
                }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            HttpContext.Session.ClearCart();

            var user = await _context.Users.FindAsync(userId);
            await _emailSender.SendAsync(
                user!.Email,
                $"Confirmación de pedido {order.OrderNumber}",
                $"Gracias por tu compra. Total: {order.TotalAmount.ToColones()}. Dirección de envío: {order.ShippingAddress}.");

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
            }

        public async Task<IActionResult> Confirmation(int id)
            {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
                {
                return NotFound();
                }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (order.UserId != userId)
                {
                return Forbid();
                }

            return View(order);
            }
        }
    }
