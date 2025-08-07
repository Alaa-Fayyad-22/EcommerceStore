using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ReviewController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int rating, string comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == user.Id);

        if (existingReview != null)
        {
            // ✅ Update existing review
            existingReview.Rating = rating;
            existingReview.Comment = comment;
            existingReview.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            // ✅ Add new review
            _context.Reviews.Add(new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Details", "Store", new { id = productId });
    }


    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Details", "Store", new { id = review.ProductId });
    }
}
