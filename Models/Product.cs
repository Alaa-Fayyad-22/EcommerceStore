using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStore.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; }
        public string Description { get; set; }
        [Required] public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }

        public List<ProductImage> Images { get; set; } = new();

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ValidateNever] // ✅ Ignore validation for navigation property
        public Category Category { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal? DiscountPrice { get; set; }
        public int SoldCount { get; set; } = 0;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();

    }

    public class ProductImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
