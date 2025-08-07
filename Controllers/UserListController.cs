using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlinePizzaWebApplication.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OnlinePizzaWebApplication.Controllers
{
    public class UserListController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserListController(UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = users.Select(user => new UserList
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(c => c.Type == "FullName")?.Value,
                DiaChi = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(c => c.Type == "DiaChi")?.Value,
                Image = _userManager.GetClaimsAsync(user).Result.FirstOrDefault(c => c.Type == "Image")?.Value,
                Role = _userManager.GetRolesAsync(user).Result.FirstOrDefault()
            }).ToList();

            return View(userList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserList model)
        {
            if (ModelState.IsValid)
            {
                string uniqueFileName = null;

                if (model.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/user");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var user = new IdentityUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber
                };

                var result = await _userManager.CreateAsync(user, "DefaultPassword123!");

                if (result.Succeeded)
                {
                    await _userManager.AddClaimAsync(user, new Claim("FullName", model.FullName ?? ""));
                    await _userManager.AddClaimAsync(user, new Claim("DiaChi", model.DiaChi ?? ""));
                    await _userManager.AddClaimAsync(user, new Claim("Image", uniqueFileName ?? ""));

                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserList
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value,
                DiaChi = claims.FirstOrDefault(c => c.Type == "DiaChi")?.Value,
                Image = claims.FirstOrDefault(c => c.Type == "Image")?.Value,
                Role = roles.FirstOrDefault()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, UserList model)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                string uniqueFileName = model.Image; // giữ nguyên nếu không thay ảnh

                if (model.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/user");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                await _userManager.UpdateAsync(user);

                var claims = await _userManager.GetClaimsAsync(user);
                foreach (var claim in claims)
                {
                    await _userManager.RemoveClaimAsync(user, claim);
                }

                await _userManager.AddClaimAsync(user, new Claim("FullName", model.FullName ?? ""));
                await _userManager.AddClaimAsync(user, new Claim("DiaChi", model.DiaChi ?? ""));
                await _userManager.AddClaimAsync(user, new Claim("Image", uniqueFileName ?? ""));

                var oldRoles = await _userManager.GetRolesAsync(user);
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, oldRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserList
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = claims.FirstOrDefault(c => c.Type == "FullName")?.Value,
                DiaChi = claims.FirstOrDefault(c => c.Type == "DiaChi")?.Value,
                Image = claims.FirstOrDefault(c => c.Type == "Image")?.Value,
                Role = roles.FirstOrDefault()
            };

            return View(model);
        }
    }
}
