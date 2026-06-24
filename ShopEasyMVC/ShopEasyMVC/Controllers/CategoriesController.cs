using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
            {
                return NotFound();
            }

            return View(category);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            category.Name = NormalizeText(category.Name);
            category.Description = NormalizeNullableText(category.Description);

            if (await CategoryNameExistsAsync(category.Name))
            {
                ModelState.AddModelError("Name", "Ya existe una categoría con este nombre.");
            }

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);

            if (category is null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            category.Name = NormalizeText(category.Name);
            category.Description = NormalizeNullableText(category.Description);

            if (await CategoryNameExistsAsync(category.Name, category.Id))
            {
                ModelState.AddModelError("Name", "Ya existe otra categoría con este nombre.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.Categories.FindAsync(id);

                    if (existingCategory is null)
                    {
                        return NotFound();
                    }

                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is not null)
            {
                if (category.Products.Any())
                {
                    ModelState.AddModelError(string.Empty, "No se puede eliminar una categoría con productos asociados.");
                    return View("Delete", category);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(c => c.Id == id);
        }

        private async Task<bool> CategoryNameExistsAsync(string name, int? categoryIdToExclude = null)
        {
            return await _context.Categories.AnyAsync(c =>
                c.Name == name && (!categoryIdToExclude.HasValue || c.Id != categoryIdToExclude.Value));
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
