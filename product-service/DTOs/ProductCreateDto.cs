using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class ProductCreateDto
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative")]
        public int StockQuantity { get; set; }

        [Required]
        [StringLength(50)]
        public required string Category { get; set; }

        [StringLength(200)]
        public string? ImageUrl { get; set; }
    }
}