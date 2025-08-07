using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Customer,Admin,SuperAdmin")]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // View Wishlist
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var wishlist = await _context.Wishlists
       .Include(w => w.Product)
       .Where(w => w.UserId == user.Id) // ✅ Filter by logged-in user
       .ToListAsync();

        return View(wishlist); // ✅ Uses Wishlist/Index.cshtml
    }

    // Add to Wishlist
    public async Task<IActionResult> Add(int productId)
    {
        var user = await _userManager.GetUserAsync(User);

        // Prevent duplicates
        if (!_context.Wishlists.Any(w => w.UserId == user.Id && w.ProductId == productId))
        {
            _context.Wishlists.Add(new Wishlist
            {
                UserId = user.Id,
                ProductId = productId
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", "Store"); // Go back to store or product details
    }

    // Remove from Wishlist
    public async Task<IActionResult> Remove(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var item = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == user.Id);
        if (item != null)
        {
            _context.Wishlists.Remove(item);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> RemoveByProduct(int productId)
    {
        var user = await _userManager.GetUserAsync(User);
        var item = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

        if (item != null)
        {
            _context.Wishlists.Remove(item);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Store", new { id = productId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromBody] WishlistToggleRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == request.ProductId);

        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            await _context.SaveChangesAsync();
            return Json(new { success = true, action = "removed" });
        }
        else
        {
            _context.Wishlists.Add(new Wishlist { UserId = user.Id, ProductId = request.ProductId });
            await _context.SaveChangesAsync();
            return Json(new { success = true, action = "added" });
        }
    }

    public class WishlistToggleRequest { public int ProductId { get; set; } }



}
