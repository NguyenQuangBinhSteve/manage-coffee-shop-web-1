using System.Diagnostics;
using manage_coffee_shop_web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace manage_coffee_shop_web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IActionResult Index(int? categoryId, string searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            var connectionString = _configuration.GetConnectionString("MyDb");
            _logger.LogInformation("Loaded connection string: {ConnectionString}", connectionString ?? "null");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Connection string 'MyDb' is not configured.");
                return View();
            }

            var banners = GetBannerFromDatabase();
            if (banners != null && banners.Any())
            {
                ViewBag.Banners = banners;
                _logger.LogInformation("Banners retrieved: Count={Count}", banners.Count);
            }
            else
            {
                ViewBag.Banners = new List<Banner>();
                _logger.LogWarning("No banner data retrieved from database.");
            }

            var categories = GetCategoriesFromDatabase();
            ViewBag.Categories = categories ?? new List<Category>(); // Ensure a valid list

            var products = string.IsNullOrEmpty(searchTerm) && !categoryId.HasValue && !minPrice.HasValue && !maxPrice.HasValue
                ? GetProductsFromDatabase() // Default products if no search parameters
                : AdvancedSearch(categoryId, searchTerm, minPrice, maxPrice); // Advanced search if parameters provided

            _logger.LogInformation("Products retrieved for Index: Count={Count}", products?.Count ?? 0);
            ViewBag.SelectedCategoryId = categoryId; // Maintain as int?
            ViewBag.SearchTerm = searchTerm;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products ?? new List<Product>());
        }

        private List<Product> GetProductsFromDatabase(int? categoryId, string searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("MyDb")))
                {
                    connection.Open();
                    var query = "SELECT TOP 6 Id, Name, Description, Price, Image FROM Products WHERE 1=1";
                    var parameters = new List<SqlParameter>();

                    if (categoryId.HasValue)
                    {
                        query += " AND CategoryId = @CategoryId";
                        parameters.Add(new SqlParameter("@CategoryId", categoryId.Value));
                    }
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query += " AND Name LIKE @SearchTerm";
                        parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                    }
                    if (minPrice.HasValue)
                    {
                        query += " AND Price >= @MinPrice";
                        parameters.Add(new SqlParameter("@MinPrice", minPrice.Value));
                    }
                    if (maxPrice.HasValue)
                    {
                        query += " AND Price <= @MaxPrice";
                        parameters.Add(new SqlParameter("@MaxPrice", maxPrice.Value));
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Price = reader.GetDecimal(3),
                                    Image = reader["Image"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products from database");
            }
            return products;
        }

        private List<Product> GetProductsFromDatabase()
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("MyDb")))
                {
                    connection.Open();
                    var query = "SELECT TOP 6 Id, Name, Description, Price, Image FROM Products ORDER BY Name";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Price = reader.GetDecimal(3),
                                    Image = reader["Image"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving default products from database");
            }
            return products;
        }

        private List<Product> AdvancedSearch(int? categoryId, string searchTerm, decimal? minPrice, decimal? maxPrice)
        {
            var products = new List<Product>();
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("MyDb")))
                {
                    connection.Open();
                    var query = "SELECT TOP 6 Id, Name, Description, Price, Image FROM Products WHERE 1=1";
                    var parameters = new List<SqlParameter>();

                    if (categoryId.HasValue)
                    {
                        query += " AND CategoryId = @CategoryId";
                        parameters.Add(new SqlParameter("@CategoryId", categoryId.Value));
                    }
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query += " AND Name LIKE @SearchTerm";
                        parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
                    }
                    if (minPrice.HasValue)
                    {
                        query += " AND Price >= @MinPrice";
                        parameters.Add(new SqlParameter("@MinPrice", minPrice.Value));
                    }
                    if (maxPrice.HasValue)
                    {
                        query += " AND Price <= @MaxPrice";
                        parameters.Add(new SqlParameter("@MaxPrice", maxPrice.Value));
                    }

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader["Name"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Price = reader.GetDecimal(3),
                                    Image = reader["Image"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products from advanced search");
            }
            return products;
        }

        private List<Category> GetCategoriesFromDatabase()
        {
            var categories = new List<Category>();
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("MyDb")))
                {
                    connection.Open();
                    var query = "SELECT Id, Name FROM Category ORDER BY Name"; // Verify this table name
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(new Category
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader["Name"].ToString()
                                });
                            }
                        }
                    }
                }
                if (categories.Count == 0)
                {
                    _logger.LogWarning("No categories found in the database. Please ensure the Category table contains data.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories from database");
            }
            return categories;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private List<Banner> GetBannerFromDatabase()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("MyDb");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string is null or empty in GetBannerFromDatabase.");
                    return new List<Banner>();
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = "SELECT Id, Title, Description, Image FROM Banner ORDER BY Id DESC";
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            var banners = new List<Banner>();
                            while (reader.Read())
                            {
                                banners.Add(new Banner
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader["Title"].ToString(),
                                    Description = reader["Description"].ToString(),
                                    Image = reader["Image"].ToString()
                                });
                            }
                            return banners;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving banners from database");
                return new List<Banner>();
            }
        }
    }
}