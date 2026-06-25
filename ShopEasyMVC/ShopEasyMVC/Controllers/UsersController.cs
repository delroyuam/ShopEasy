using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;
using System.Text.RegularExpressions;

namespace ShopEasyMVC.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(user => user.UserRoles)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                return NotFound();
            }

            return View(user);
        }

        public async Task<IActionResult> Create()
        {
            await LoadRolesSelectListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,PasswordHash")] User user, string roleName)
        {
            user.Email = NormalizeEmail(user.Email);
            roleName = NormalizeRoleName(roleName);

            if (await EmailExistsAsync(user.Email))
            {
                ModelState.AddModelError("Email", "Ya existe un usuario con este correo.");
            }

            if (!IsValidRoleName(roleName))
            {
                ModelState.AddModelError("roleName", "Debe seleccionar un rol valido.");
            }
            else if (!await RoleCatalogExistsAsync(roleName))
            {
                ModelState.AddModelError("roleName", "Debe seleccionar un rol existente.");
            }

            if (ModelState.IsValid)
            {
                var passwordHasher = new PasswordHasher<User>();
                user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);
                user.UserRoles.Add(new UserRole { Name = roleName });

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadRolesSelectListAsync(roleName);
            return View(user);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user is null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            user.Email = NormalizeEmail(user.Email);
            ModelState.Remove("PasswordHash");

            if (await EmailExistsAsync(user.Email, user.Id))
            {
                ModelState.AddModelError("Email", "Ya existe otro usuario con este correo.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);

                    if (existingUser is null)
                    {
                        return NotFound();
                    }

                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user is not null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(u => u.Id == id);
        }

        private async Task<bool> EmailExistsAsync(string email, int? userIdToExclude = null)
        {
            return await _context.Users.AnyAsync(u =>
                u.Email == email && (!userIdToExclude.HasValue || u.Id != userIdToExclude.Value));
        }

        private async Task<bool> RoleCatalogExistsAsync(string roleName)
        {
            return await _context.UserRoles.AnyAsync(role =>
                role.UserId == null && role.Name == roleName);
        }

        private async Task LoadRolesSelectListAsync(string? selectedRoleName = null)
        {
            var roles = await _context.UserRoles
                .Where(role => role.UserId == null)
                .OrderBy(role => role.Name)
                .Select(role => role.Name)
                .ToListAsync();

            ViewData["RoleName"] = new SelectList(roles, selectedRoleName);
        }

        private static string NormalizeEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string NormalizeRoleName(string? roleName)
        {
            return (roleName ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool IsValidRoleName(string roleName)
        {
            return Regex.IsMatch(roleName, UserRole.NamePattern);
        }
    }
}
