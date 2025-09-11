using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using manage_coffee_shop_web.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace manage_coffee_shop_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")] // Restrict to Admin/Manager
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext context, ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // List feedback with pagination
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            var feedbacks = await _context.Feedbacks
                .Where(f => !f.IsDeleted)
                .Include(f => f.ApplicationUser)
                .Include(f => f.Product)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalItems = await _context.Feedbacks.CountAsync(f => !f.IsDeleted);
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;

            return View(feedbacks);
        }

        // View feedback details
        public async Task<IActionResult> Details(int id)
        {
            var feedback = await _context.Feedbacks
                .Where(f => !f.IsDeleted)
                .Include(f => f.ApplicationUser)
                .Include(f => f.Product)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (feedback == null)
            {
                _logger.LogWarning("Feedback with ID {Id} not found.", id);
                return NotFound();
            }

            if (feedback.Status == Feedback.FeedbackState.New)
            {
                feedback.Status = Feedback.FeedbackState.Read;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Feedback {Id} status updated to Read by {UserId}", id, User.Identity.Name);
            }

            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var feedback = await _context.Feedbacks
                    .Where(f => !f.IsDeleted)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (feedback == null)
                {
                    _logger.LogWarning("Attempt to delete non-existent feedback with ID {Id}", id);
                    return NotFound();
                }

                // Explicitly mark the entity as modified
                _context.Entry(feedback).Property(f => f.IsDeleted).IsModified = true;
                feedback.IsDeleted = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Feedback {Id} deleted by {UserId} at {Time}", id, User.Identity.Name, DateTime.UtcNow);
                TempData["DeleteSuccess"] = "Phản hồi đã được xóa thành công.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error updating feedback {Id} to deleted state", id);
                TempData["DeleteError"] = "Đã xảy ra lỗi khi xóa phản hồi. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting feedback {Id}", id);
                TempData["DeleteError"] = "Đã xảy ra lỗi không mong muốn. Vui lòng liên hệ quản trị viên.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}