using System.Security.Claims;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly LMSDB _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(LMSDB db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // --- FIX 1: Use full name 'LibraryManagementSystem.Models.User' ---
                    if (user.Status == LibraryManagementSystem.Models.User.UserStatus.LOCKED ||
                        user.Status == LibraryManagementSystem.Models.User.UserStatus.INACTIVE)
                    {
                        ModelState.AddModelError(string.Empty, "Account is locked or inactive.");
                        return View(model);
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role.ToString()),
                        new Claim("ProfilePicture", user.ImageURL ?? "default-profile.png")

                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                if (await _db.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    return View(model);
                }

                if (await _db.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    return View(model);
                }

                // IMAGE UPLOAD LOGIC
                string stringFileName = "default-profile.png";

                if (model.ProfilePhoto != null)
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                    Directory.CreateDirectory(uploadFolder);

                    stringFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePhoto.FileName;
                    string filePath = Path.Combine(uploadFolder, stringFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePhoto.CopyToAsync(fileStream);
                    }
                }

                var newUserId = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();

                var user = new User
                {
                    Id = newUserId,
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    ContactNo = model.ContactNo,
                    // --- FIX 2: Use full name here as well ---
                    Role = LibraryManagementSystem.Models.User.UserRole.MEMBER,
                    Status = LibraryManagementSystem.Models.User.UserStatus.ACTIVE,
                    ImageURL = stringFileName
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Auto Login
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("ProfilePicture", user.ImageURL ?? "default-profile.png")
                };


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}