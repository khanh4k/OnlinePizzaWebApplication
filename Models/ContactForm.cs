using System.ComponentModel.DataAnnotations;

namespace OnlinePizzaWebApplication.Models
{
    public class ContactForm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề.")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung.")]
        public string Message { get; set; }
    }
}

