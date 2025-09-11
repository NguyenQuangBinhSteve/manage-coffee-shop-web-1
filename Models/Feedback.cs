using System.ComponentModel.DataAnnotations;

namespace manage_coffee_shop_web.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } // Khóa ngoại đến ApplicationUser

        [Required] // Khóa ngoại đến Product
        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string Comment { get; set; } // Nội dung phản hồi

        [Range(1, 5)]
        public int Rating { get; set; } // Điểm đánh giá (1-5)

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tạo

        public ApplicationUser ApplicationUser { get; set; } // Navigation property
        public Product Product { get; set; } // Navigation property added
                                             // New properties for workflow

        // Status property using the renamed enum
        public FeedbackState Status { get; set; } = FeedbackState.New; // Default to New
        public bool IsDeleted { get; set; } = false; // Soft delete flag

        // Renamed enum to avoid conflict
        public enum FeedbackState
        {
            New,
            Read,
            Processing,
            Closed
        }
    }
}