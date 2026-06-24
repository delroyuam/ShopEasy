using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly AppDbContext _context;

        public UserRolesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: UserRoles
        public async Task<IActionResult> Index()
        {
            var userRoles = await _context.UserRoles
                .Include(userRole => userRole.User)
                .ToListAsync();

            return View(userRoles);
        }

        // GET: UserRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .Include(role => role.User)
                .FirstOrDefaultAsync(role => role.Id == id);

            if (userRole is null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        // GET: UserRoles/Create
        public async Task<IActionResult> Create()
        {
            await LoadUsersSelectListAsync();
            return View();
        }

        // POST: UserRoles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,UserId")] UserRole userRole)
        {
            userRole.Name = NormalizeRoleName(userRole.Name);

            ModelState.Remove("User");

            await ValidateUserRoleAsync(userRole);

            if (ModelState.IsValid)
            {
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadUsersSelectListAsync(userRole.UserId);
            return View(userRole);
        }

        // GET: UserRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles.FindAsync(id);

            if (userRole is null)
            {
                return NotFound();
            }

            await LoadUsersSelectListAsync(userRole.UserId);
            return View(userRole);
        }

        // POST: UserRoles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,UserId")] UserRole userRole)
        {
            if (id != userRole.Id)
            {
                return NotFound();
            }

            userRole.Name = NormalizeRoleName(userRole.Name);

            ModelState.Remove("User");

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

            await LoadUsersSelectListAsync(userRole.UserId);
            return View(userRole);
        }

        // GET: UserRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var userRole = await _context.UserRoles
                .Include(role => role.User)
                .FirstOrDefaultAsync(role => role.Id == id);

            if (userRole is null)
            {
                return NotFound();
            }

            return View(userRole);
        }

        // POST: UserRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRole = await _context.UserRoles.FindAsync(id);

            if (userRole is not null)
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
            var userExists = await _context.Users.AnyAsync(user => user.Id == userRole.UserId);

            if (!userExists)
            {
                ModelState.AddModelError("UserId", "Debe seleccionar un usuario válido.");
                return;
            }

            var duplicatedRole = await _context.UserRoles.AnyAsync(role =>
                role.UserId == userRole.UserId
                && role.Name == userRole.Name
                && (!userRoleIdToExclude.HasValue || role.Id != userRoleIdToExclude.Value));

            if (duplicatedRole)
            {
                ModelState.AddModelError("Name", "Este usuario ya tiene asignado ese rol.");
            }
        }

        private async Task LoadUsersSelectListAsync(int? selectedUserId = null)
        {
            var users = await _context.Users
                .OrderBy(user => user.FullName)
                .Select(user => new
                {
                    user.Id,
                    DisplayName = user.FullName + " (" + user.Email + ")"
                })
                .ToListAsync();

            ViewData["UserId"] = new SelectList(users, "Id", "DisplayName", selectedUserId);
        }

        private static string NormalizeRoleName(string? roleName)
        {
            return (roleName ?? string.Empty).Trim();
        }
    }
}
