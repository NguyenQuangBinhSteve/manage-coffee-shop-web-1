using manage_coffee_shop_web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace manage_coffee_shop_web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TransactionHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string customerName, DateTime? startDate, DateTime? endDate, decimal? minTotal, decimal? maxTotal)
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(customerName))
            {
                orders = orders.Where(o => o.ApplicationUser.UserName.Contains(customerName));
                ViewBag.CustomerName = customerName;
            }
            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= startDate.Value);
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            }
            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= endDate.Value);
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            }
            if (minTotal.HasValue)
            {
                orders = orders.Where(o => o.TotalAmount >= minTotal.Value);
                ViewBag.MinTotal = minTotal.Value;
            }
            if (maxTotal.HasValue)
            {
                orders = orders.Where(o => o.TotalAmount <= maxTotal.Value);
                ViewBag.MaxTotal = maxTotal.Value;
            }

            var model = orders.ToList();
            return View(model);
        }

        [HttpPost]
        public IActionResult Archive(int[] orderIds)
        {
            if (orderIds == null || !orderIds.Any())
            {
                return RedirectToAction("Index");
            }

            var ordersToArchive = _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => orderIds.Contains(o.Id))
                .ToList();

            foreach (var order in ordersToArchive)
            {
                var archivedOrder = new ArchivedOrder
                {
                    ApplicationUserId = order.ApplicationUserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    ArchivedDate = DateTime.Now
                };
                _context.ArchivedOrders.Add(archivedOrder);

                foreach (var detail in order.OrderDetails)
                {
                    var archivedDetail = new ArchivedOrderDetail
                    {
                        OrderId = archivedOrder.Id,
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Price = detail.Price,
                        Notes = detail.Notes
                    };
                    _context.ArchivedOrderDetails.Add(archivedDetail);
                }

                _context.Orders.Remove(order);
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("Details", order); // Explicitly specify the view name
        }
    }
}