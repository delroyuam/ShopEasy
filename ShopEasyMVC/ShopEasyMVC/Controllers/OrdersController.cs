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
                var previousStatus = order.Status;
                order.Status = status;
                await AdjustStockForStatusChangeAsync(order.Id, previousStatus, status);
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

            var now = DateTime.UtcNow;

            return View(new Order
            {
                CreatedAt = now,
                Status = OrderStatus.Pending,
                OrderNumber = await GenerateOrderNumberAsync(now.Year)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TotalAmount,Status,CreatedAt,UserId")] Order order)
        {
            order.OrderNumber = await GenerateOrderNumberAsync(order.CreatedAt.Year);

            ModelState.Remove("User");
            ModelState.Remove("OrderItems");
            ModelState.Remove("OrderNumber");

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

                    var previousStatus = existingOrder.Status;

                    existingOrder.OrderNumber = order.OrderNumber;
                    existingOrder.TotalAmount = order.TotalAmount;
                    existingOrder.Status = order.Status;
                    existingOrder.CreatedAt = order.CreatedAt;
                    existingOrder.UserId = order.UserId;

                    await AdjustStockForStatusChangeAsync(existingOrder.Id, previousStatus, order.Status);
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
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (order.Status != OrderStatus.Cancelled)
            {
                ModelState.AddModelError(string.Empty, "Solo se pueden eliminar órdenes canceladas.");
                return View("Delete", order);
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(o => o.Id == id);
        }

        private async Task AdjustStockForStatusChangeAsync(int orderId, OrderStatus previousStatus, OrderStatus newStatus)
        {
            var becameCancelled = newStatus == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled;
            var leftCancelled = previousStatus == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled;

            if (!becameCancelled && !leftCancelled)
            {
                return;
            }

            var items = await _context.OrderItems
                .Include(item => item.Product)
                .Where(item => item.OrderId == orderId)
                .ToListAsync();

            foreach (var item in items)
            {
                if (item.Product is null)
                {
                    continue;
                }

                if (becameCancelled)
                {
                    item.Product.Stock += item.Quantity;
                }
                else
                {
                    item.Product.Stock -= item.Quantity;
                }
            }
        }

        private async Task<string> GenerateOrderNumberAsync(int year)
        {
            var prefix = $"ORD-{year}-";

            var existingNumbers = await _context.Orders
                .Where(o => o.OrderNumber.StartsWith(prefix))
                .Select(o => o.OrderNumber)
                .ToListAsync();

            var maxSequence = 0;

            foreach (var number in existingNumbers)
            {
                if (int.TryParse(number.AsSpan(prefix.Length), out var sequence) && sequence > maxSequence)
                {
                    maxSequence = sequence;
                }
            }

            return $"{prefix}{maxSequence + 1:D3}";
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
