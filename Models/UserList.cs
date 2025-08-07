using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace OnlinePizzaWebApplication.Models
{
    public class UserList
    {
        public string Id { get; set; }

        [Display(Name = "Tên người dùng")]
        public string UserName { get; set; }

        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string Image { get; set; }

        [Display(Name = "Quyền")]
        public string Role { get; set; }

        // Nhận file ảnh upload từ form
        public IFormFile ImageFile { get; set; }
    }
}
