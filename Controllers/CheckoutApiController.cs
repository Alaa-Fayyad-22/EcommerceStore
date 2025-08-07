using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceStore.API
{
    [Route("api/checkout")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CheckoutApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CheckoutApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null || !cart.Items.Any()) return BadRequest("Cart is empty");

            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = cart.Items.Sum(i => i.Product.Price * i.Quantity),
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            return Ok(order);
        }
    }
}
