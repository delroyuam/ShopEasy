using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;
using System.Text.RegularExpressions;

namespace ShopEasyMVC.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly AppDbContext _context;

        public UserRolesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userRoles = await _context.UserRoles
                .Where(userRole => userRole.UserId == null)
                .OrderBy(userRole => userRole.Name)
                .ToListAsync();

            return View(userRoles);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(role => role.Id == id && role.UserId == null);

            if (userRole is null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] UserRole userRole)
        {
            userRole.Name = NormalizeRoleName(userRole.Name);

            ModelState.Remove("User");
            ModelState.Remove("UserId");

            await ValidateUserRoleAsync(userRole);

            if (ModelState.IsValid)
            {
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(userRole);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles.FindAsync(id);

            if (userRole is null || userRole.UserId.HasValue)
            {
                return NotFound();
            }

            return View(userRole);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] UserRole userRole)
        {
            if (id != userRole.Id)
            {
                return NotFound();
            }

            userRole.Name = NormalizeRoleName(userRole.Name);
            userRole.UserId = null;

            ModelState.Remove("User");
            ModelState.Remove("UserId");

            await ValidateUserRoleAsync(userRole, userRole.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUserRole = await _context.UserRoles.FindAsync(id);

                    if (existingUserRole is null)
                    {
                        return NotFound();
                    }

                    existingUserRole.Name = userRole.Name;
                    existingUserRole.UserId = userRole.UserId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserRoleExists(userRole.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(userRole);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(role => role.Id == id && role.UserId == null);

            if (userRole is null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRole = await _context.UserRoles.FindAsync(id);

            if (userRole is not null && userRole.UserId == null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserRoleExists(int id)
        {
            return _context.UserRoles.Any(role => role.Id == id);
        }

        private async Task ValidateUserRoleAsync(UserRole userRole, int? userRoleIdToExclude = null)
        {
            if (!IsValidRoleName(userRole.Name))
            {
                ModelState.AddModelError("Name", "El rol solo puede contener letras y espacios.");
                return;
            }

            if (userRole.UserId.HasValue)
            {
                var userExists = await _context.Users.AnyAsync(user => user.Id == userRole.UserId.Value);

                if (!userExists)
                {
                    ModelState.AddModelError("UserId", "Debe seleccionar un usuario valido.");
                    return;
                }
            }

            var duplicatedRole = userRole.UserId.HasValue
                ? await _context.UserRoles.AnyAsync(role =>
                    role.UserId == userRole.UserId.Value
                    && role.Name == userRole.Name
                    && (!userRoleIdToExclude.HasValue || role.Id != userRoleIdToExclude.Value))
                : await _context.UserRoles.AnyAsync(role =>
                    role.UserId == null
                    && role.Name == userRole.Name
                    && (!userRoleIdToExclude.HasValue || role.Id != userRoleIdToExclude.Value));

            if (duplicatedRole)
            {
                ModelState.AddModelError("Name", userRole.UserId.HasValue
                    ? "Este usuario ya tiene asignado ese rol."
                    : "Este rol ya existe.");
            }
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
