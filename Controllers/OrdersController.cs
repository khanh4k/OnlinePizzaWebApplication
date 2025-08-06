using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlinePizzaWebApplication.Repositories;
using OnlinePizzaWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OnlinePizzaWebApplication.Data;
using OnlinePizzaWebApplication.Service;
using OnlinePizzaWebApplication.ViewModels;

namespace OnlinePizzaWebApplication.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ShoppingCart _shoppingCart;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IVnPayService _vnPayService;

        public OrdersController(IOrderRepository orderRepository,
            ShoppingCart shoppingCart, AppDbContext context, UserManager<IdentityUser> userManager,
            IVnPayService vnPayService)
        {
            _orderRepository = orderRepository;
            _shoppingCart = shoppingCart;
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
        }

        [Authorize]
        public IActionResult Checkout()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var items = await _shoppingCart.GetShoppingCartItemsAsync();
            _shoppingCart.ShoppingCartItems = items;

            if (_shoppingCart.ShoppingCartItems.Count == 0)
            {
                ModelState.AddModelError("", "Your cart is empty, add some pizzas first");
                return View(order);
            }

            if (ModelState.IsValid)
            {
                order.UserId = userId;
                order.OrderPlaced = DateTime.Now;
                order.OrderTotal = _shoppingCart.ShoppingCartItems.Sum(item => item.Pizza.Price * item.Amount);
                order.Status = "Pending COD";

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                ViewBag.CheckoutCompleteMessage = $"Thanks for your order {order.OrderId}! We'll deliver your pizzas soon!";
                return View("CheckoutComplete");
            }

            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> CheckoutWithVNPay(Order order)
        {
            var userId = _userManager.GetUserId(HttpContext.User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var items = await _shoppingCart.GetShoppingCartItemsAsync();
            _shoppingCart.ShoppingCartItems = items;

            if (_shoppingCart.ShoppingCartItems.Count == 0)
            {
                ModelState.AddModelError("", "Your cart is empty, add some pizzas first");
                return View("Checkout", order);
            }

            if (!ModelState.IsValid)
            {
                return View("Checkout", order);
            }

            // Lưu Order vào database
            order.UserId = userId;
            order.OrderPlaced = DateTime.Now;
            order.OrderTotal = _shoppingCart.ShoppingCartItems.Sum(item => item.Pizza.Price * item.Amount);
            order.Status = "Pending Payment";

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo yêu cầu thanh toán VNPay
            var vnPayModel = new VnPaymentRequestModel
            {
                OrderId = order.OrderId.ToString(),
                FullName = $"{order.FirstName} {order.LastName}",
                Description = "Order from OnlinePizzaWebApplication",
                Amount = (double)order.OrderTotal,
                CreatedDate = order.OrderPlaced
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel);
            return Redirect(paymentUrl);
        }

        [Authorize]
        public IActionResult CheckoutComplete()
        {
            ViewBag.CheckoutCompleteMessage = $"Thanks for your order, We'll deliver your pizzas soon!";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback()
        {
            var response = Request.Query;
            string vnp_TransactionStatus = response["vnp_TransactionStatus"];
            string vnp_TxnRef = response["vnp_TxnRef"];
            string vnp_Amount = response["vnp_Amount"];

            Console.WriteLine($"PaymentCallback - vnp_TxnRef: {vnp_TxnRef}, vnp_TransactionStatus: {vnp_TransactionStatus}, vnp_Amount: {vnp_Amount}");

            if (!string.IsNullOrEmpty(vnp_TransactionStatus) && vnp_TransactionStatus == "00")
            {
                if (int.TryParse(vnp_TxnRef, out int orderId))
                {
                    var order = await _context.Orders.FindAsync(orderId);
                    if (order != null)
                    {
                        order.Status = "Paid VNPay";
                        order.OrderTotal = decimal.Parse(vnp_Amount) / 100;
                        await _context.SaveChangesAsync();
                    }
                }
                ViewBag.CheckoutCompleteMessage = "Thanks for your order! Payment VNPay was successful. We'll deliver your pizzas soon!";
                return View("CheckoutComplete");
            }
            else
            {
                Console.WriteLine($"Payment failed or invalid status: {vnp_TransactionStatus}");
                return RedirectToAction("Checkout");
            }
        }

        // GET: Reviews
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var allOrders = await _context.Orders.Include(o => o.OrderLines).Include(o => o.User).ToListAsync();
                return View(allOrders);
            }
            else
            {
                var orders = await _context.Orders.Include(o => o.OrderLines).Include(o => o.User)
                    .Where(r => r.User == user).ToListAsync();
                return View(orders);
            }
        }

        // GET: Orders/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var orders = await _context.Orders.Include(o => o.OrderLines).Include(o => o.User)
                .SingleOrDefaultAsync(m => m.OrderId == id);
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var userRoles = await _userManager.GetRolesAsync(user);
            bool isAdmin = userRoles.Any(r => r == "Admin");

            if (orders == null)
            {
                return NotFound();
            }

            if (!isAdmin)
            {
                var userId = _userManager.GetUserId(HttpContext.User);
                if (orders.UserId != userId)
                {
                    return BadRequest("You do not have permissions to view this order.");
                }
            }

            var orderDetailsList = _context.OrderDetails.Include(o => o.Pizza).Include(o => o.Order)
                .Where(x => x.OrderId == orders.OrderId);

            ViewBag.OrderDetailsList = orderDetailsList;

            return View(orders);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Orders/Edit/5
        public IActionResult Edit(int id)
        {
            return View();
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here
                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.Include(o => o.User)
                .SingleOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: OrdersTest/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.SingleOrDefaultAsync(m => m.OrderId == id);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}