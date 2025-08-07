using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EcommerceStore.API
{
    [Route("api/cart")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CartApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public CartApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("view")]
        public async Task<IActionResult> ViewCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.Include(c => c.Items).ThenInclude(i => i.Product)
                        .FirstOrDefaultAsync(c => c.UserId == userId);
            return Ok(cart);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId)
                       ?? new Cart { UserId = userId, Items = new List<CartItem>() };

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
                cartItem.Quantity += quantity;
            else
                cart.Items.Add(new CartItem { ProductId = productId, Quantity = quantity });

            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
            return Ok(cart);
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null) return NotFound();

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
                cart.Items.Remove(item);

            await _context.SaveChangesAsync();
            return Ok(cart);
        }
    }
}
