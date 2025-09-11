using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace manage_coffee_shop_web.Models
{
    public class ArchivedOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } // FK to ApplicationUser

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        [Required]
        public DateTime ArchivedDate { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<ArchivedOrderDetail> ArchivedOrderDetails { get; set; } = new List<ArchivedOrderDetail>();
    }
}