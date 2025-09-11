using manage_coffee_shop_web.Models;
using manage_coffee_shop_web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace manage_coffee_shop_web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    PhoneNumber = model.Phone,
                    Address = model.Address
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign default "User" role
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }
                    await _userManager.AddToRoleAsync(user, "User");

                    TempData["RegisterSuccess"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                TempData["RegisterFailure"] = "Đăng ký thất bại. Vui lòng thử lại.";
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        TempData["LoginSuccess"] = "Đăng nhập thành công!";
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "AdminDashBoard", new { area = "Admin" });
                        }
                        return RedirectToLocal(returnUrl);
                    }
                }
                ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu.");
                TempData["LoginFailure"] = "Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.";
            }
            return View(model);
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            // Generate OTP (6 digits)
            var otp = new Random().Next(100000, 999999).ToString();
            var otpExpiry = DateTime.UtcNow.AddMinutes(5);

            // Store OTP in session
            HttpContext.Session.SetString("Otp", otp);
            HttpContext.Session.SetString("OtpExpiry", otpExpiry.ToString("o"));
            HttpContext.Session.SetString("UserEmail", model.Email);

            // Send OTP via Gmail SMTP
            var smtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    _configuration["Email:Settings:Username"],
                    _configuration["Email:Settings:Password"]
                )
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:Settings:Username"]),
                Subject = "Mã Xác Nhận Đặt Lại Mật Khẩu",
                Body = $"Mã xác nhận của bạn là: {otp}. Mã có hiệu lực trong 5 phút.",
                IsBodyHtml = true
            };
            mailMessage.To.Add(model.Email);

            await smtpClient.SendMailAsync(mailMessage);

            return RedirectToAction("VerifyOtp");
        }

        // GET: /Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            return View();
        }

        // POST: /Account/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var storedOtp = HttpContext.Session.GetString("Otp");
            var otpExpiryStr = HttpContext.Session.GetString("OtpExpiry");
            var expiry = DateTime.Parse(otpExpiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind);

            if (storedOtp != model.Otp || DateTime.UtcNow > expiry)
            {
                ModelState.AddModelError("Otp", "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng thử lại.");
                return View(model);
            }

            return RedirectToAction("ResetPassword");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            // Generate temporary password (9 characters)
            var tempPassword = GenerateRandomPassword(9);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

            if (result.Succeeded)
            {
                // Send temporary password via email
                var smtpClient = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        _configuration["Email:Settings:Username"],
                        _configuration["Email:Settings:Password"]
                    )
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["Email:Settings:Username"]),
                    Subject = "Mật Khẩu Tạm Thời",
                    Body = $"Mật khẩu tạm thời của bạn là: {tempPassword}. Vui lòng đăng nhập và đổi mật khẩu ngay.",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                HttpContext.Session.Remove("Otp");
                HttpContext.Session.Remove("OtpExpiry");
                HttpContext.Session.Remove("UserEmail");
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Có lỗi xảy ra khi đặt lại mật khẩu.");
            return View("VerifyOtp");
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                return RedirectToAction("ChangePasswordSuccess");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // GET: /Account/ChangePasswordSuccess
        [HttpGet]
        public IActionResult ChangePasswordSuccess()
        {
            return View();
        }

        private string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}