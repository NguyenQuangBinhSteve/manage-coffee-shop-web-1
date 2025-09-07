using manage_coffee_shop_web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

public class ProductController : Controller
{
    private readonly ILogger<ProductController> _logger;
    private readonly IConfiguration _configuration;

    public ProductController(ILogger<ProductController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        var products = GetProductsFromDatabase();
        ViewBag.Products = products;
        return View("~/Views/Home/Index.cshtml");
    }

    [Authorize] // Restricts access to authenticated users only
    public IActionResult Details(int id)
    {
        var products = GetProductsFromDatabase();
        var product = products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {Id} not found.", id);
            return NotFound();
        }
        ViewBag.ShowNotification = !User.Identity.IsAuthenticated;
        return View(product);
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
                var query = "SELECT Id, Name, Price, Image, Description FROM Products ORDER BY Id DESC";
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
                                Description = reader["Description"].ToString()
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
}