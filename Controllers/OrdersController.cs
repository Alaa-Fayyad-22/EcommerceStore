using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStore.Controllers
{
    [Authorize] // ✅ All logged-in users can access this controller, role filtering happens per action
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ GET: Orders List (Customers see their orders, Admins/SuperAdmins see all)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product);

            if (User.IsInRole("Customer"))
            {
                // Customers see only their own orders
                query = query.Where(o => o.UserId == user.Id);
            }
            // Admin/SuperAdmin automatically see all orders (no filter)

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ✅ GET: Order Details (accessible to all, restricted per user)
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Customers can only view their own orders
            if (User.IsInRole("Customer") && order.UserId != _userManager.GetUserId(User))
            {
                return Forbid();
            }

            return View(order);
        }

        // ✅ GET: Edit Order (Admins/SuperAdmins only)
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // ✅ POST: Edit Order (Admins/SuperAdmins only)
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // ✅ GET: Delete Confirmation (Admins/SuperAdmins only)
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // ✅ POST: Delete Order (Admins/SuperAdmins only)
        [HttpPost, ActionName("DeleteConfirmed")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ POST: Update Status (Admins/SuperAdmins only)
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            if (!ModelState.IsValid)
                return View(order);

            // Save the order first
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Update stock for each ordered product
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    if (product.Stock >= item.Quantity)
                    {
                        product.Stock -= item.Quantity;  // Reduce stock
                    }
                    else
                    {
                        // Handle insufficient stock (optional)
                        ModelState.AddModelError("", $"Not enough stock for {product.Name}");
                        return View(order);
                    }
                }
            }

            await _context.SaveChangesAsync();  // Save stock updates

            // Clear cart after successful order
            var userId = _userManager.GetUserId(User);
            var cartItems = _context.Carts.Where(c => c.UserId == userId);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order placed successfully!";
            return RedirectToAction("OrderConfirmation");
        }

        [HttpPost]
        public IActionResult TestPlaceOrder()
        {
            Console.WriteLine("🔥 TestPlaceOrder HIT!");
            return Ok("It works!");
        }

    }
}
