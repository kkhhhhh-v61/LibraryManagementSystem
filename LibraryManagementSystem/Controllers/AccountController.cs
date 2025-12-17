using System.Security.Claims;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LMSUser = LibraryManagementSystem.Models.User; 

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

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
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
                    if (user.Status == LMSUser.UserStatus.LOCKED || user.Status == LMSUser.UserStatus.INACTIVE)
                    {
                        ModelState.AddModelError(string.Empty, "Account is locked or inactive.");
                        return View(model);
                    }

                    await SignInUser(user);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                // specific validation checks
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

                // ---------------------------------------------------------
                // ID Generation Logic (Format: U00001, U00002...)
                // ---------------------------------------------------------

                // Retrieve the most recent user ID that matches the 'U' pattern
                var lastUser = await _db.Users
                    .Where(u => u.Id.StartsWith("U"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefaultAsync();

                string newUserId;

                if (lastUser == null)
                {
                    // Initialize sequence if no previous users exist
                    newUserId = "U00001";
                }
                else
                {
                    string numericPart = lastUser.Id.Substring(1);
                    if (int.TryParse(numericPart, out int currentIdNum))
                    {
                        newUserId = "U" + (currentIdNum + 1).ToString("D5");
                    }
                    else
                    {
                        // Fallback to start of sequence if parsing fails
                        newUserId = "U00001";
                    }
                }

                // ---------------------------------------------------------
                // Image Upload Logic
                // ---------------------------------------------------------
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

                // Create new user entity
                var user = new LMSUser
                {
                    Id = newUserId,
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    ContactNo = model.ContactNo,
                    Role = LMSUser.UserRole.MEMBER,
                    Status = LMSUser.UserStatus.ACTIVE,
                    ImageURL = stringFileName
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // Automatically sign in the user after registration
                await SignInUser(user);
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

       
        private async Task SignInUser(LMSUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("ProfilePicture", user.ImageURL ?? "default-profile.png")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        }
    }
}