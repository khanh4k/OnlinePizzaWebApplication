using Microsoft.AspNetCore.Mvc;
using OnlinePizzaWebApplication.Models;

namespace OnlinePizzaWebApplication.Controllers
{
    public class ContactController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(ContactForm model)
        {
            if (ModelState.IsValid)
            {
                // Xử lý gửi email hoặc lưu vào DB tại đây (tùy bạn)
                ViewBag.SuccessMessage = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";
                ModelState.Clear(); // Xóa form sau khi gửi thành công
            }
            return View();
        }
    }
}
