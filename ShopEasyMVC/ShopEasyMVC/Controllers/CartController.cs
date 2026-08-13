using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopEasyMVC.Data;
using ShopEasyMVC.Helpers;

namespace ShopEasyMVC.Controllers
    {
    [Authorize]
    public class CartController : Controller
        {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
            {
            _context = context;
            }

        public IActionResult Index()
            {
            return View(HttpContext.Session.GetCart());
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
            {
            var product = await _context.Products.FindAsync(productId);

            if (product is null || !product.IsActive)
                {
                return NotFound();
                }

            var cart = HttpContext.Session.GetCart();
            var line = cart.FirstOrDefault(l => l.ProductId == productId);
            var currentQuantity = line?.Quantity ?? 0;
            var newQuantity = Math.Min(currentQuantity + Math.Max(quantity, 1), product.Stock);

            if (newQuantity <= currentQuantity)
                {
                TempData["CartMessage"] = "No hay suficiente stock disponible.";
                return RedirectToAction("Details", "Products", new { id = productId });
                }

            if (line is null)
                {
                cart.Add(new Models.CartLine
                    {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.CurrentPrice,
                    Quantity = newQuantity
                    });
                }
            else
                {
                line.Quantity = newQuantity;
                }

            HttpContext.Session.SaveCart(cart);
            return RedirectToAction(nameof(Index));
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
            {
            var cart = HttpContext.Session.GetCart();
            var line = cart.FirstOrDefault(l => l.ProductId == productId);

            if (line is not null)
                {
                if (quantity <= 0)
                    {
                    cart.Remove(line);
                    }
                else
                    {
                    var product = await _context.Products.FindAsync(productId);
                    line.Quantity = product is null ? quantity : Math.Min(quantity, product.Stock);
                    }

                HttpContext.Session.SaveCart(cart);
                }

            return RedirectToAction(nameof(Index));
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
            {
            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll(l => l.ProductId == productId);
            HttpContext.Session.SaveCart(cart);

            return RedirectToAction(nameof(Index));
            }
        }
    }
