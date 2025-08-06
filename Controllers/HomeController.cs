using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OnlinePizzaWebApplication.Models;

namespace OnlinePizzaWebApplication.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var pizzas = new List<Pizzas>
                {
                    new Pizzas { Name = "Margherita", Price = 8.99m, Description = "Classic pizza with tomato and cheese", ImageUrl = "/images/margherita.jpg" },
                    new Pizzas { Name = "Pepperoni", Price = 9.99m, Description = "Spicy pepperoni with mozzarella", ImageUrl = "/images/pepperoni.jpg" }
                };

            return View(pizzas);
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

    }
}
