using manage_coffee_shop_web.Extensions;
using manage_coffee_shop_web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
public class ProductController : Controller
{
    private readonly ILogger<ProductController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context; // DbContext field
    private readonly UserManager<ApplicationUser> _userManager; // Added UserManager injection
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductController(ILogger<ProductController> logger, IConfiguration configuration, ApplicationDbContext context, UserManager<ApplicationUser> userManager,IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _configuration = configuration;
        _context = context; // Assign injected ApplicationDbContext
        _userManager = userManager; // Inject UserManager
        _httpContextAccessor = httpContextAccessor;
    }

    public IActionResult Index(int? categoryId, string searchTerm, decimal? minPrice, decimal? maxPrice)
    {
        List<Product> products = null;
        try
        {
            products = GetProductsFromDatabase();
            _logger.LogInformation("Total products retrieved for Index: {Count}", products?.Count ?? 0);

            // Apply filters based on query parameters
            var filteredProducts = products.AsQueryable();
            if (categoryId.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryId == categoryId.Value); // Safe cast since HasValue is checked
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredProducts = filteredProducts.Where(p => p.Name.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }
            if (minPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);
                ViewBag.MinPrice = minPrice.Value;
            }
            if (maxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice.Value;
            }

            // Get categories for the dropdown
            var categories = GetCategoriesFromDatabase();
            ViewBag.Categories = categories;

            var model = filteredProducts.Take(6).ToList(); // Take 6 products after filtering
            _logger.LogInformation("Filtered model count for Index view: {Count}", model.Count);
            return View("~/Views/Home/Index.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving or filtering products for Index action");
            ViewBag.Categories = GetCategoriesFromDatabase() ?? new List<Category>();
            var model = new List<Product>(); // Fallback to empty list on error
            return View("~/Views/Home/Index.cshtml", model);
        }
    }

    public IActionResult Details(int id)
    {
        var products = GetProductsFromDatabase();
        var product = products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {Id} not found.", id);
            return NotFound();
        }

        // Load feedback for the product, excluding deleted records
        var feedbacks = _context.Feedbacks
            .Where(f => f.ProductId == id && !f.IsDeleted)
            .ToList();

        ViewBag.Feedbacks = feedbacks; // Pass feedback to the view via ViewBag
        return View(product);
    }

    [Authorize] // Restrict to authenticated users
    [HttpPost]
    public async Task<IActionResult> CreateFeedback(int productId, string comment, int rating)
    {
        if (string.IsNullOrEmpty(comment) || rating < 1 || rating > 5)
        {
            TempData["FeedbackError"] = "Bình luận không được để trống và đánh giá phải từ 1 đến 5 sao.";
            return RedirectToAction("Details", new { id = productId });
        }

        // Validate that the product exists
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            _logger.LogWarning("Attempted to create feedback for non-existent product ID {ProductId}", productId);
            TempData["FeedbackError"] = "Sản phẩm không tồn tại. Vui lòng thử lại.";
            return RedirectToAction("Details", new { id = productId });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogError("Authenticated user not found for feedback submission.");
            TempData["FeedbackError"] = "Người dùng không được xác định. Vui lòng đăng nhập lại.";
            return RedirectToAction("Details", new { id = productId });
        }

        var feedback = new Feedback
        {
            ApplicationUserId = user.Id,
            ProductId = productId,
            Comment = comment,
            Rating = rating,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Feedback submitted by user {UserId} for product {ProductId}", user.Id, productId);
            TempData["FeedbackSuccess"] = "Cảm ơn bạn đã gửi phản hồi!";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving feedback for product {ProductId}", productId);
            TempData["FeedbackError"] = "Đã xảy ra lỗi khi lưu phản hồi. Vui lòng thử lại.";
        }

        return RedirectToAction("Details", new { id = productId });
    }

    public IActionResult AddToCart(int id)
    {
        var products = GetProductsFromDatabase();
        var product = products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {Id} not found for adding to cart.", id);
            return NotFound();
        }

        var cart = GetOrCreateCart();
        var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);
        if (cartItem != null)
        {
            cartItem.Quantity++;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                ProductId = id,
                Product = product,
                Quantity = 1,
                Price = product.Price
            });
        }
        _logger.LogInformation("Product {Id} added to cart by user {User}", id, User.Identity?.Name ?? "Guest");

        HttpContext.Session.SetObjectAsJson("Cart", cart);
        return RedirectToAction("Cart");
    }

    public IActionResult Cart()
    {
        var cart = GetOrCreateCart();
        ViewBag.Cart = cart;
        return View(cart);
    }

    
    public IActionResult CartDetail()
    {
        var cart = GetOrCreateCart();
        if (!cart.CartItems.Any())
        {
            _logger.LogWarning("Cart is empty for user {User}", User.Identity?.Name);
            return RedirectToAction("Cart");
        }
        ViewBag.Cart = cart; // Pass cart to layout for display
        return View(cart);
    }

    [Authorize]
    public IActionResult Checkout()
    {
        var cart = GetOrCreateCart();
        if (!cart.CartItems.Any())
        {
            _logger.LogWarning("Cart is empty for user {User}", User.Identity.Name);
            return RedirectToAction("Cart");
        }
        ViewBag.Cart = cart;
        return View(cart);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CheckoutConfirmed() // Changed to async for UserManager
    {
        var cart = GetOrCreateCart();
        if (!cart.CartItems.Any())
        {
            _logger.LogWarning("Cart is empty for user {User}", User.Identity.Name);
            return RedirectToAction("Cart");
        }

        var user = await _userManager.GetUserAsync(User); // Get the current user
        if (user == null)
        {
            _logger.LogError("User not found for {UserName}", User.Identity.Name);
            return RedirectToAction("Cart");
        }

        var order = new Order
        {
            ApplicationUserId = user.Id, // Use the user's Id from AspNetUsers
            OrderDate = DateTime.Now,
            TotalAmount = cart.CartItems.Sum(ci => ci.Price * ci.Quantity),
            Status = "Pending"
        };
        _context.Orders.Add(order);
        _context.SaveChanges();

        foreach (var cartItem in cart.CartItems)
        {
            var orderDetail = new OrderDetail
            {
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Price = cartItem.Price,
                Notes = cartItem.Notes
            };
            _context.OrderDetails.Add(orderDetail);
        }
        _context.SaveChanges();

        _logger.LogInformation("Checkout confirmed for user {User} with total {Total}", User.Identity.Name, cart.CartItems.Sum(ci => ci.Price * ci.Quantity));
        cart.CartItems.Clear();
        HttpContext.Session.SetObjectAsJson("Cart", cart);
        TempData["Message"] = "Đặt hàng thành công!";
        return RedirectToAction("Index", "Home");
    }

    private Cart GetOrCreateCart()
    {
        var cart = HttpContext.Session.GetObjectFromJson<Cart>("Cart") ?? new Cart();
        if (User.Identity?.IsAuthenticated ?? false)
        {
            cart.ApplicationUserId = User.Identity.Name; // Associate with authenticated user
        }
        return cart;
    }

    public IActionResult Category()
    {
        var products = GetProductsFromDatabase();
        _logger.LogInformation("Products retrieved for Category: {Count}", products?.Count ?? 0);
        return View(products);
    }

    [HttpPost]
    public IActionResult DeleteSelectedItems(List<int> itemIds)
    {
        var cart = GetOrCreateCart();
        if (itemIds != null && itemIds.Any())
        {
            // Remove items with the selected ProductIds
            var itemsToRemove = cart.CartItems.Where(ci => itemIds.Contains(ci.ProductId)).ToList();
            foreach (var item in itemsToRemove)
            {
                cart.CartItems.Remove(item);
            }
            _logger.LogInformation("Deleted {Count} items from cart for user {User}", itemsToRemove.Count, User.Identity?.Name ?? "Guest");
        }
        HttpContext.Session.SetObjectAsJson("Cart", cart);
        return RedirectToAction("CartDetail");
    }


    private List<Product> GetProductsFromDatabase()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MyDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string is null or empty in GetProductsFromDatabase.");
                return new List<Product>();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, Price, Image, Description, CategoryId FROM Products ORDER BY Id DESC";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var products = new List<Product>();
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                Name = reader["Name"].ToString(),
                                Price = reader.GetDecimal(2),
                                Image = reader["Image"].ToString(),
                                Description = reader["Description"].ToString(),
                                CategoryId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5) // Correctly handles nullable CategoryId
                            });
                        }
                        return products;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products from database");
            return new List<Product>();
        }
    }

    private List<Category> GetCategoriesFromDatabase()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("MyDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string is null or empty in GetCategoriesFromDatabase.");
                return new List<Category>();
            }

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name FROM Categories ORDER BY Name";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var categories = new List<Category>();
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                Id = reader.GetInt32(0),
                                Name = reader["Name"].ToString()
                            });
                        }
                        return categories;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories from database");
            return new List<Category>();
        }
    }
}

// Extension methods for session
public static class SessionExtensions
{
    public static T GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
    }

    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
    }
}