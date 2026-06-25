using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;

        // Umbral para marcar un producto como "stock bajo" en el panel de inventario.
        private const int LowStockThreshold = 5;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // RF-008 - Gestión de Inventario (Admin): listado de productos con búsqueda y filtros.
        public async Task<IActionResult> Index(string? search, int? categoryId, string? stock)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                productsQuery = productsQuery.Where(p => p.Name.Contains(term));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            productsQuery = stock switch
            {
                "out" => productsQuery.Where(p => p.Stock == 0),
                "low" => productsQuery.Where(p => p.Stock > 0 && p.Stock <= LowStockThreshold),
                "in" => productsQuery.Where(p => p.Stock > LowStockThreshold),
                _ => productsQuery
            };

            var products = await productsQuery
                .OrderBy(p => p.Name)
                .ToListAsync();

            await LoadCategoriesSelectListAsync(categoryId);
            ViewData["Search"] = search;
            ViewData["StockFilter"] = stock;
            ViewData["LowStockThreshold"] = LowStockThreshold;

            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
            {
                return NotFound();
            }

            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            await LoadCategoriesSelectListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,CurrentPrice,Stock,ImageUrl,IsActive,CategoryId")] Product product)
        {
            product.Name = NormalizeText(product.Name);
            product.Description = NormalizeNullableText(product.Description);
            product.ImageUrl = NormalizeNullableText(product.ImageUrl);
            product.CreatedAtUtc = DateTime.UtcNow;

            ModelState.Remove("Category");

            await ValidateProductAsync(product);

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesSelectListAsync(product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);

            if (product is null)
            {
                return NotFound();
            }

            await LoadCategoriesSelectListAsync(product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,CurrentPrice,Stock,ImageUrl,IsActive,CategoryId")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            product.Name = NormalizeText(product.Name);
            product.Description = NormalizeNullableText(product.Description);
            product.ImageUrl = NormalizeNullableText(product.ImageUrl);

            ModelState.Remove("Category");

            await ValidateProductAsync(product, product.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.FindAsync(id);

                    if (existingProduct is null)
                    {
                        return NotFound();
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.CurrentPrice = product.CurrentPrice;
                    existingProduct.Stock = product.Stock;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.IsActive = product.IsActive;
                    existingProduct.CategoryId = product.CategoryId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesSelectListAsync(product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product is not null)
            {
                var hasOrderItems = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);

                if (hasOrderItems)
                {
                    ModelState.AddModelError(string.Empty, "No se puede eliminar un producto que ya está en una orden.");
                    return View("Delete", product);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(p => p.Id == id);
        }

        private async Task ValidateProductAsync(Product product, int? productIdToExclude = null)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError("CategoryId", "Debe seleccionar una categoría válida.");
            }

            var duplicatedName = await _context.Products.AnyAsync(p =>
                p.Name == product.Name && (!productIdToExclude.HasValue || p.Id != productIdToExclude.Value));

            if (duplicatedName)
            {
                ModelState.AddModelError("Name", "Ya existe un producto con este nombre.");
            }
        }

        private async Task LoadCategoriesSelectListAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name", selectedCategoryId);
        }

        private static string NormalizeText(string? text)
        {
            return (text ?? string.Empty).Trim();
        }

        private static string? NormalizeNullableText(string? text)
        {
            var normalizedText = text?.Trim();
            return string.IsNullOrWhiteSpace(normalizedText) ? null : normalizedText;
        }
    }
}
