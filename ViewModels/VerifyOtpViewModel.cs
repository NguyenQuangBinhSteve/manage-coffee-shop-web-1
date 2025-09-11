using System.ComponentModel.DataAnnotations;

namespace manage_coffee_shop_web.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 ký tự.")]
        public string Otp { get; set; }
    }
}