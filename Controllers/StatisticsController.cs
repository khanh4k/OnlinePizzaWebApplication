using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlinePizzaWebApplication.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlinePizzaWebApplication.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? month, int? year, string type)
        {
            var orders = _context.Orders.AsQueryable();

            // Lọc bỏ đơn có ngày đặt hoặc tổng tiền null
            orders = orders.Where(o => o.OrderPlaced != null && o.OrderTotal != null);

            // Lọc theo năm
            if (year.HasValue)
                orders = orders.Where(o => o.OrderPlaced.Year == year.Value);

            // Lọc theo tháng
            if (month.HasValue)
                orders = orders.Where(o => o.OrderPlaced.Month == month.Value);

            // Lọc theo loại đơn (nếu có cột Type hoặc tương tự)
            if (!string.IsNullOrEmpty(type))
                orders = orders.Where(o => GetOrderType(o) == type);

            // Tính tổng số đơn
            var totalOrders = await orders.CountAsync();

            // Tính tổng doanh thu, tránh null
            var totalRevenue = await orders.SumAsync(o => (decimal?)o.OrderTotal) ?? 0;

            // Gom nhóm theo ngày, xử lý null an toàn
            var chartData = await orders
                .GroupBy(o => o.OrderPlaced.Date)  // OrderPlaced chắc chắn không null vì đã lọc ở trên
                .Select(g => new
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.OrderTotal)
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // Chuẩn bị dữ liệu cho view
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Labels = chartData.Select(c => c.Date.ToString("dd/MM/yyyy")).ToArray();
            ViewBag.OrderCounts = chartData.Select(c => c.OrderCount).ToArray();
            ViewBag.Revenues = chartData.Select(c => c.Revenue).ToArray();

            return View();
        }

        // Giả sử có thuộc tính OrderType hoặc Category trong Order
        private static string GetOrderType(Models.Order o)
        {
            // Nếu không có thì trả về null hoặc một giá trị mặc định
            return o.OrderType;
        }
    }
}
