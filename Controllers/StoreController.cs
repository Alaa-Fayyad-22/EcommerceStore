using EcommerceStore.Data;
using EcommerceStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

[AllowAnonymous]
public class StoreController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StoreController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ✅ Product Listing with Search, Filtering, Sorting, Pagination
    public async Task<IActionResult> Index(string search, int? categoryId, decimal? minPrice, decimal? maxPrice, string sort, int page = 1, int pageSize = 8)
    {
        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .AsQueryable();

        // 🔎 Search filter
        if (!string.IsNullOrEmpty(search))
        {
            productsQuery = productsQuery.Where(p =>
                p.Name.Contains(search) || p.Description.Contains(search));
        }

        // 🏷 Category filter
        if (categoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);

        // 💲 Price range filter
        if (minPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

        if (sort == "bestseller")
            productsQuery = productsQuery.OrderByDescending(p => p.SoldCount);

        // 🔀 Sorting
        productsQuery = sort switch
        {
            "price_asc" => productsQuery.OrderBy(p => p.Price),
            "price_desc" => productsQuery.OrderByDescending(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            _ => productsQuery.OrderBy(p => p.Name)
        };

        // ✅ Pagination
        var totalProducts = await productsQuery.CountAsync();
        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ✅ Wishlist check
        List<int> wishlistIds = new();
        if (User.Identity.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);
            wishlistIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();
        }

        // ✅ Best Sellers & Featured Products
        var bestSellers = await _context.Products
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.SoldCount)
            //.OrderByDescending(p => p.Reviews.Count)
            .Take(8)
            .ToListAsync();

        var featuredProducts = await _context.Products
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        ViewBag.WishlistIds = wishlistIds;
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.BestSellers = bestSellers;
        ViewBag.Featured = featuredProducts;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

        return View(products);
    }

    // ✅ Product Details Page
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // ✅ Wishlist state
        if (User.Identity.IsAuthenticated)
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.IsInWishlist = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == id);
        }
        else
        {
            ViewBag.IsInWishlist = false;
        }

        // ✅ Related Products
        var relatedProducts = await _context.Products
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
            .Take(4)
            .ToListAsync();
        ViewBag.RelatedProducts = relatedProducts;

        // ✅ Recently Viewed Products (session-based)
        var recentlyViewed = HttpContext.Session.GetString("RecentlyViewed");
        List<int> viewedIds = string.IsNullOrEmpty(recentlyViewed)
            ? new List<int>()
            : JsonSerializer.Deserialize<List<int>>(recentlyViewed);

        if (!viewedIds.Contains(id))
        {
            viewedIds.Insert(0, id);
            if (viewedIds.Count > 5) viewedIds = viewedIds.Take(5).ToList();
            HttpContext.Session.SetString("RecentlyViewed", JsonSerializer.Serialize(viewedIds));
        }

        var recentlyViewedProducts = await _context.Products
            .Where(p => viewedIds.Contains(p.Id) && p.Id != id)
            .ToListAsync();
        ViewBag.RecentlyViewed = recentlyViewedProducts;

        return View(product);
    }

}
