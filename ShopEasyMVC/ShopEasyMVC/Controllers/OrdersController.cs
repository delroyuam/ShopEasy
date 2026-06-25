using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopEasyMVC.Data;
using ShopEasyMVC.Helpers;
using ShopEasyMVC.Models;

namespace ShopEasyMVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // RF-009 - Gestión de Pedidos (Admin): lista todos los pedidos con filtro opcional por estado.
        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (status.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status.Value);
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            LoadStatusFilterList(status);

            return View(orders);
        }

        // RF-009 - Cambio rápido de estado del pedido desde la lista (sin abrir el formulario completo).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status, OrderStatus? filter)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order is null)
            {
                return NotFound();
            }

            if (!Enum.IsDefined(status))
            {
                ModelState.AddModelError(string.Empty, "El estado seleccionado no es válido.");
            }
            else
            {
                order.Status = status;
                await _context.SaveChangesAsync();

                TempData["StatusMessage"] = $"La orden {order.OrderNumber} cambió a estado «{status.GetDisplayName()}».";
            }

            return RedirectToAction(nameof(Index), new { status = filter });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            await LoadUsersSelectListAsync();
            LoadStatusSelectList();

            return View(new Order
            {
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderNumber,TotalAmount,Status,CreatedAt,UserId")] Order order)
        {
            order.OrderNumber = NormalizeText(order.OrderNumber);

            ModelState.Remove("User");
            ModelState.Remove("OrderItems");

            await ValidateOrderAsync(order);

            if (ModelState.IsValid)
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadUsersSelectListAsync(order.UserId);
            LoadStatusSelectList(order.Status);
            return View(order);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);

            if (order is null)
            {
                return NotFound();
            }

            await LoadUsersSelectListAsync(order.UserId);
            LoadStatusSelectList(order.Status);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderNumber,TotalAmount,Status,CreatedAt,UserId")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            order.OrderNumber = NormalizeText(order.OrderNumber);

            ModelState.Remove("User");
            ModelState.Remove("OrderItems");

            await ValidateOrderAsync(order, order.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Orders.FindAsync(id);

                    if (existingOrder is null)
                    {
                        return NotFound();
                    }

                    existingOrder.OrderNumber = order.OrderNumber;
                    existingOrder.TotalAmount = order.TotalAmount;
                    existingOrder.Status = order.Status;
                    existingOrder.CreatedAt = order.CreatedAt;
                    existingOrder.UserId = order.UserId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadUsersSelectListAsync(order.UserId);
            LoadStatusSelectList(order.Status);
            return View(order);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order is not null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(o => o.Id == id);
        }

        private async Task ValidateOrderAsync(Order order, int? orderIdToExclude = null)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == order.UserId);

            if (!userExists)
            {
                ModelState.AddModelError("UserId", "Debe seleccionar un usuario válido.");
            }

            var duplicatedOrderNumber = await _context.Orders.AnyAsync(o =>
                o.OrderNumber == order.OrderNumber && (!orderIdToExclude.HasValue || o.Id != orderIdToExclude.Value));

            if (duplicatedOrderNumber)
            {
                ModelState.AddModelError("OrderNumber", "Ya existe una orden con este número.");
            }
        }

        private async Task LoadUsersSelectListAsync(int? selectedUserId = null)
        {
            var users = await _context.Users
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    DisplayName = u.FullName + " (" + u.Email + ")"
                })
                .ToListAsync();

            ViewData["UserId"] = new SelectList(users, "Id", "DisplayName", selectedUserId);
        }

        private void LoadStatusSelectList(OrderStatus? selectedStatus = null)
        {
            ViewData["Status"] = BuildStatusItems(selectedStatus);
        }

        // Lista de estados (en español) usada por el filtro y el cambio rápido en el Index.
        private void LoadStatusFilterList(OrderStatus? selectedStatus = null)
        {
            ViewData["StatusFilter"] = BuildStatusItems(selectedStatus);
        }

        private static List<SelectListItem> BuildStatusItems(OrderStatus? selectedStatus)
        {
            return Enum.GetValues<OrderStatus>()
                .Select(status => new SelectListItem
                {
                    Value = status.ToString(),
                    Text = status.GetDisplayName(),
                    Selected = selectedStatus.HasValue && selectedStatus.Value == status
                })
                .ToList();
        }

        private static string NormalizeText(string? text)
        {
            return (text ?? string.Empty).Trim();
        }
    }
}
