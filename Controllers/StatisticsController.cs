using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlinePizzaWebApplication.Data;
using Microsoft.AspNetCore.Mvc.Rendering; // Thêm namespace này
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlinePizzaWebApplication.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index(int? month, int? year, string type)
        {
            if (_context == null)
            {
                throw new InvalidOperationException("AppDbContext is not initialized.");
            }

            var orders = _context.Orders.AsQueryable();

            // Lọc bỏ đơn có ngày đặt hoặc tổng tiền null
            orders = orders.Where(o => o.OrderPlaced != null && o.OrderTotal != null);

            // Lọc theo năm
            if (year.HasValue)
                orders = orders.Where(o => o.OrderPlaced.Year == year.Value);

            // Lọc theo tháng
            if (month.HasValue)
                orders = orders.Where(o => o.OrderPlaced.Month == month.Value);

            // Lấy danh sách OrderId liên quan đến Pizza đã chọn
            var orderIds = new List<int>();
            if (!string.IsNullOrEmpty(type))
            {
                var pizzaIds = await _context.OrderDetails
                    .Where(od => od.Pizza != null && od.Pizza.Name == type)
                    .Select(od => od.OrderId)
                    .Distinct()
                    .ToListAsync();
                orderIds = pizzaIds;
            }

            // Lấy dữ liệu từ database
            var filteredOrders = await orders
                .Include(o => o.OrderLines)
                .ToListAsync();

            // Lọc thêm theo OrderId nếu có type
            if (orderIds.Any())
            {
                filteredOrders = filteredOrders
                    .Where(o => orderIds.Contains(o.OrderId))
                    .ToList();
            }

            // Tính tổng số đơn (đếm OrderId)
            var totalOrders = filteredOrders.Count;

            // Tính tổng doanh thu (tổng OrderTotal)
            var totalRevenue = filteredOrders.Sum(o => (decimal?)o.OrderTotal) ?? 0;

            // Gom nhóm theo ngày cho biểu đồ
            var chartData = filteredOrders
                .GroupBy(o => o.OrderPlaced.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.OrderTotal)
                })
                .OrderBy(g => g.Date)
                .ToList();

            // Lấy danh sách các Pizza đã được đặt
            var orderedPizzaNames = await _context.OrderDetails
                .Include(od => od.Pizza)
                .Select(od => od.Pizza.Name)
                .Distinct()
                .ToListAsync();
            ViewBag.PizzaNames = orderedPizzaNames;

            // Chuẩn bị danh sách tháng (1-12)
            var months = Enumerable.Range(1, 12).Select(m => new SelectListItem { Value = m.ToString(), Text = m.ToString("D2") }).ToList();
            ViewBag.Months = months;

            // Chuẩn bị danh sách năm (dựa trên dữ liệu OrderPlaced, nếu rỗng thì dùng 2020-2025)
            var yearsFromData = (await _context.Orders
                .Where(o => o.OrderPlaced != null)
                .Select(o => o.OrderPlaced.Year)
                .Distinct()
                .ToListAsync())
                .OrderBy(y => y)
                .Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() })
                .ToList();
            var years = yearsFromData.Any() ? yearsFromData : Enumerable.Range(2020, 6).Select(y => new SelectListItem { Value = y.ToString(), Text = y.ToString() }).ToList();
            ViewBag.Years = years;

            // Chuẩn bị dữ liệu cho view
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Labels = chartData.Select(c => c.Date.ToString("dd/MM/yyyy")).ToArray();
            ViewBag.OrderCounts = chartData.Select(c => c.OrderCount).ToArray();
            ViewBag.Revenues = chartData.Select(c => c.Revenue).ToArray();

            return View();
        }

        // Giả sử có thuộc tính OrderType hoặc Category trong Order (không dùng nữa)
        private static string GetOrderType(Models.Order o)
        {
            return o.OrderType;
        }
    }
}