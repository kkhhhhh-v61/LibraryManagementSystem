using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LMSUser = LibraryManagementSystem.Models.User;
using LibraryManagementSystem.Services;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly LMSDB _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;

        public AccountController(LMSDB db, IWebHostEnvironment webHostEnvironment, IEmailSender emailSender, IMemoryCache cache)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
            _cache = cache; 
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =======================================================
        // LOGIN SECTION (UNCHANGED)
        // =======================================================
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
                // Find the user in the database
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user == null)
                {
                    TempData["ToastMessage"] = "Invalid username or password.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
                {
                    var remainingMins = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);

                    TempData["ToastMessage"] = $"Account locked. Try again in {remainingMins} minutes.";
                    TempData["ToastType"] = "error"; // Red
                    return View(model);
                }

                if (user.Status == LMSUser.UserStatus.INACTIVE)
                {
                    TempData["ToastMessage"] = "Your account is inactive. Please contact support.";
                    TempData["ToastType"] = "warning";
                    return View(model);
                }

                //Verify Password
                if (BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // --- SUCCESS ---

                    if (user.FailedLoginAttempts > 0 || user.LockoutEnd != null)
                    {
                        user.FailedLoginAttempts = 0;
                        user.LockoutEnd = null;

                        // If they were locked but time passed, set back to ACTIVE
                        if (user.Status == LMSUser.UserStatus.LOCKED)
                        {
                            user.Status = LMSUser.UserStatus.ACTIVE;
                        }

                        await _db.SaveChangesAsync();
                    }

                    // Standard Sign In
                    await SignInUser(user);

                    TempData["ToastMessage"] = "Welcome back, " + user.Username + "!";
                    TempData["ToastType"] = "success";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // --- FAILURE (Wrong Password) ---

                    // Increment failed attempts
                    user.FailedLoginAttempts++;
                    int maxAttempts = 3;

                    if (user.FailedLoginAttempts >= maxAttempts)
                    {
                        // Lock the user
                        user.Status = LMSUser.UserStatus.LOCKED;
                        user.LockoutEnd = DateTime.Now.AddMinutes(15); // Lock for 15 mins

                        await _db.SaveChangesAsync();

                        TempData["ToastMessage"] = "Account locked! Too many failed attempts.";
                        TempData["ToastType"] = "error";
                    }
                    else
                    {
                        // Warn the user
                        int attemptsLeft = maxAttempts - user.FailedLoginAttempts;
                        await _db.SaveChangesAsync();

                        TempData["ToastMessage"] = $"Invalid password. You have {attemptsLeft} attempt(s) remaining.";
                        TempData["ToastType"] = "warning"; // Yellow/Orange
                    }

                    return View(model);
                }
            }

            return View(model);
        }

        // =======================================================
        // REGISTER SECTION (ADDED WITH RECAPTCHA)
        // =======================================================
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
            // --- reCAPTCHA Verification ---
            string recaptchaToken = Request.Form["g-recaptcha-response"];
            string secretKey = "6LcE1C4sAAAAAEoFEeRtXToUjbY8smzSWozqj-_N";

            bool isHuman = await Helper.VerifyReCaptcha(recaptchaToken, secretKey);
            if (!isHuman)
            {
                TempData["CaptchaError"] = "Please verify that you are not a robot.";
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // ==================================================
                // DUPLICATE DATA CHECKS
                // ==================================================
                bool hasDuplicate = false; 

                // Check Username
                if (await _db.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already taken.");
                    hasDuplicate = true;
                }

                // Check Email
                if (await _db.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    hasDuplicate = true;
                }

                // Check Contact Number
                if (await _db.Users.AnyAsync(u => u.ContactNo == model.ContactNo))
                {
                    ModelState.AddModelError("ContactNo", "Phone number already in use.");
                    hasDuplicate = true;
                }

                // IF DUPLICATES FOUND: Show Toast and Stop
                if (hasDuplicate)
                {
                    TempData["ToastMessage"] = "Registration failed! One or more details are already registered.";
                    TempData["ToastType"] = "error"; // Shows Red Toast
                    return View(model);
                }

                // Generate User ID
                string newUserId = "U00001";
                var lastUser = await _db.Users.IgnoreQueryFilters()
                    .Where(u => u.Id.StartsWith("U"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefaultAsync();

                if (lastUser != null && int.TryParse(lastUser.Id.Substring(1), out int lastIdNum))
                {
                    newUserId = "U" + (lastIdNum + 1).ToString("D5");
                }

                // Handle Image (Using Helper)
                string stringFileName = "default-profile.png";

                if (model.ProfilePhoto != null)
                {
                    var imageValidation = Helper.ValidateImage(model.ProfilePhoto);
                    if (imageValidation != ValidationResult.Success)
                    {
                        ModelState.AddModelError("ProfilePhoto", imageValidation.ErrorMessage);
                        // Also show a toast for image error if you want
                        TempData["ToastMessage"] = "Invalid Profile Photo.";
                        TempData["ToastType"] = "error";
                        return View(model);
                    }

                    // Save and get filename
                    stringFileName = Helper.SaveImage(model.ProfilePhoto, "profiles", _webHostEnvironment.WebRootPath);
                }

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

                await SignInUser(user);
                TempData["ToastMessage"] = "Account created successfully!";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "Home");
            }

            // If we got here, normal validation failed (e.g. password too short)
            TempData["ToastMessage"] = "Please check your input.";
            TempData["ToastType"] = "warning";
            return View(model);
        }
        // =======================================================
        // PASSWORD RECOVERY SECTION (NEW)
        // =======================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (ModelState.IsValid)
            {
                // =========================================================
                // 1. RATE LIMIT CHECK (Max 3 attempts per 20 mins)
                // =========================================================
                string cacheKey = $"ResetLimit_{model.Email}";

                if (_cache.TryGetValue(cacheKey, out int currentCount))
                {
                    if (currentCount >= 3)
                    {
                        TempData["ToastMessage"] = "Too many attempts. Please wait 60 minutes before trying again.";
                        TempData["ToastType"] = "warning";
                        return RedirectToAction("Login");
                    }
                }

                // =========================================================
                // 2. NORMAL PASSWORD RESET LOGIC
                // =========================================================
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && user.Status != LMSUser.UserStatus.INACTIVE)
                {
                    var oldTokens = _db.PasswordResetTokens.Where(t => t.UserId == user.Id);
                    _db.PasswordResetTokens.RemoveRange(oldTokens);

                    string tokenString = Guid.NewGuid().ToString();
                    var resetToken = new PasswordResetToken
                    {
                        UserId = user.Id,
                        Token = tokenString,
                        ExpiryDate = DateTime.Now.AddMinutes(15),
                        IsUsed = false
                    };

                    _db.PasswordResetTokens.Add(resetToken);
                    await _db.SaveChangesAsync();

                    // C. Send Email
                    string resetLink = Url.Action("ResetPassword", "Account",
                        new { token = tokenString, email = model.Email }, Request.Scheme) ?? "";

                    string emailBody = $@"
                <h3>Reset Your Password</h3>
                <p>Hello {user.Username},</p>
                <p>Click the link below to reset your password (Valid for 15 mins):</p>
                <p><a href='{resetLink}' style='padding:10px; background-color:#007bff; color:white; text-decoration:none; border-radius:5px;'>Reset Password</a></p>";

                    await _emailSender.SendEmailAsync(model.Email, "Reset Password Request", emailBody);
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(60));

                    _cache.Set(cacheKey, currentCount + 1, cacheOptions);
                }

                TempData["ToastMessage"] = "If that email exists, a reset link has been sent!";
                TempData["ToastType"] = "success";

                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                TempData["ToastMessage"] = "Invalid password reset link.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordVM { Token = token, Email = email };
            return View(model);
        }
        // =======================================================
        // RESET PASSWORD
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var resetToken = await _db.PasswordResetTokens
                .Include(t => t.User) 
                .FirstOrDefaultAsync(t => t.Token == model.Token && t.User.Email == model.Email);

            //Validate Token
            if (resetToken == null)
            {
                TempData["ToastMessage"] = "Invalid token.";
                return RedirectToAction("Login");
            }
            if (resetToken.IsUsed)
            {
                TempData["ToastMessage"] = "This link has already been used.";
                return RedirectToAction("Login");
            }
            if (resetToken.ExpiryDate < DateTime.Now)
            {
                TempData["ToastMessage"] = "This link has expired.";
                return RedirectToAction("Login");
            }

            //Update Password (Using BCrypt to match your Register logic)
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            //Mark token as used
            resetToken.IsUsed = true;

            //Clear any locks the user might have had
            resetToken.User.FailedLoginAttempts = 0;
            resetToken.User.LockoutEnd = null;
            if (resetToken.User.Status == LMSUser.UserStatus.LOCKED)
            {
                resetToken.User.Status = LMSUser.UserStatus.ACTIVE;
            }

            _db.Update(resetToken.User);
            _db.Update(resetToken);
            await _db.SaveChangesAsync();

            TempData["ToastMessage"] = "Password reset successful! Please login.";
            TempData["ToastType"] = "success";

            return RedirectToAction("Login");
        }

        // =======================================================
        // LOGOUT 
        // =======================================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            string username = User.Identity?.Name ?? "User";

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["ToastMessage"] = "See you, " + username + "!";
            TempData["ToastType"] = "info";

            return RedirectToAction("Login", "Account");
        }

        private async Task SignInUser(LMSUser user)
        {   
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                // If ImageURL is null, use a default placeholder
                new Claim("ProfilePicture", user.ImageURL ?? "default-profile.png")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        }
    }
}