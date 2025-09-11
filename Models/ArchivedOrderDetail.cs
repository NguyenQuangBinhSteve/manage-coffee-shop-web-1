using System.ComponentModel.DataAnnotations;

namespace manage_coffee_shop_web.Models
{
    public class ArchivedOrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; } // FK to ArchivedOrder

        [Required]
        public int ProductId { get; set; } // FK to Product

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, (double)decimal.MaxValue)]
        public decimal Price { get; set; }

        [StringLength(200)]
        public string Notes { get; set; }

        public ArchivedOrder ArchivedOrder { get; set; }
        public Product Product { get; set; }
    }
}