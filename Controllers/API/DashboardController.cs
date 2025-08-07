using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStore.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")] // ✅ Only Admin & SuperAdmin
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get All Products
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Name,
                    price = p.Price,
                    costprice = p.Price * 0.6M, // Example cost price logic
                    quantity = p.Stock,
                    category = p.Category.Name,
                    image = p.ImageUrl
                })
                .ToListAsync();

            return Ok(products);
        }

        //// ✅ Get All Orders
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    id = o.Id,
                    userId = o.UserId,
                    date = o.CreatedAt.ToString("yyyy-MM-dd"),
                    products = o.OrderItems.Select(i => new
                    {
                        productId = i.ProductId,
                        quantity = i.Quantity
                    }),
                    total = o.TotalAmount,
                    status = o.Status
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ✅ Get All Customers
        [HttpGet("user")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _context.Users
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    username = u.UserName,
                    fullname = u.FullName,
                    address = u.Address,
                    status = (u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow) ? "Inactive" : "Active", // ✅ Added

                    created_date = u.CreatedAt.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(customers);
        }
        // ✅ Get Single Product by ID
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Name,
                    price = p.Price,
                    costprice = p.Price * 0.6M,
                    quantity = p.Stock,
                    category = p.Category.Name,
                    image = p.ImageUrl,
                    description = p.Description
                })
                .FirstOrDefaultAsync();

            if (product == null) return NotFound();
            return Ok(product);
        }

        //// ✅ Get Single Order by ID
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)         // ✅ Use OrderItems
                    .ThenInclude(i => i.Product)
                .Where(o => o.Id == id)
                .Select(o => new
                {
                    id = o.Id,
                    userId = o.UserId,
                    date = o.CreatedAt.ToString("yyyy-MM-dd"),
                    products = o.OrderItems.Select(i => new    // ✅ Use OrderItems here too
                    {
                        productId = i.ProductId,
                        quantity = i.Quantity,
                        productName = i.Product.Name,          // optional extra field
                        price = i.Product.Price               // optional extra field
                    }),
                    total = o.TotalAmount,
                    status = o.Status
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound();
            return Ok(order);
        }


        //// ✅ Get Single Customer by ID
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetCustomer(string id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "Customer not found." });

            // Get CreatedAt property safely (nullable handling)
            DateTime? createdAt = EF.Property<DateTime?>(user, "CreatedAt");

            var customer = new
            {
                id = user.Id,
                email = user.Email,
                username = user.UserName,
                fullname = user.FullName,
                address = user.Address,
                status = (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow) ? "Inactive" : "Active", // ✅ Added
                created_date = user.CreatedAt.ToString("yyyy-MM-dd")
            };

            return Ok(customer);
        }


    }
}
