using System.ComponentModel.DataAnnotations;

namespace EcommerceStore.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Linked to ApplicationUser
        public ApplicationUser User { get; set; }

        [Required]
        public int ProductId { get; set; } // Linked to Product
        public Product Product { get; set; }

    }
}
