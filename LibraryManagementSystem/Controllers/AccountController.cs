using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LMSUser = LibraryManagementSystem.Models.User;

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
                // Use .IgnoreQueryFilters() to see if the user exists but is deleted
                var user = await _db.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user == null) { /* Standard invalid login logic */ }

                // CHECK IF DELETED
                if (user.IsDeleted)
                {
                    TempData["ToastMessage"] = "This account has been deleted.";
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
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            resetToken.IsUsed = true;
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
            // 1. Get the Role Name (e.g., "ADMIN")
            string roleName = user.Role.ToString();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, roleName),
        new Claim(ClaimTypes.Role, roleName[0] + roleName.Substring(1).ToLower()),
        new Claim("ProfilePicture", user.ImageURL ?? "default-profile.png")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                // Keep user logged in for 30 days
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
        [HttpGet]
        [Authorize] // Ensures only logged-in users can see this
        public async Task<IActionResult> Profile()
        {
            // Get current user ID
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var model = new ProfileVM
            {
                Username = user.Username,
                Role = user.Role.ToString(),
                CredibilityScore = user.CredibilityScore,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ContactNo = user.ContactNo,
                IsProfilePublic = user.IsProfilePublic,
                ProfilePhotoUrl = user.ImageURL
            };

            return View(model);
        }
        // =======================================================
        // 1. UPDATE PROFILE (Fixed for Custom Auth / LMSDB)
        // =======================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileVM model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Validation failed.", errors = errors });
            }

            // 1. Get the current user ID from Claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "User not logged in." });

            // 2. Fetch User from DB
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            // ==========================================
            // CHECK FOR DUPLICATES (Only if changed)
            // ==========================================

            // 3. Check Email (Only if the user changed it)
            if (model.Email.ToLower() != user.Email.ToLower())
            {
                bool emailExists = await _db.Users.AnyAsync(u => u.Email == model.Email && u.Id != user.Id);
                if (emailExists)
                {
                    return Json(new { success = false, message = "Email already in use by another account." });
                }
            }

            // 4. Check Phone (Only if the user changed it)
            if (model.ContactNo != user.ContactNo)
            {
                bool phoneExists = await _db.Users.AnyAsync(u => u.ContactNo == model.ContactNo && u.Id != user.Id);
                if (phoneExists)
                {
                    return Json(new { success = false, message = "Phone number already in use by another account." });
                }
            }

            // ==========================================
            // UPDATE FIELDS
            // ==========================================
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.ContactNo = model.ContactNo;
            user.IsProfilePublic = model.IsProfilePublic;

            // Handle Email Update specifically
            if (model.Email != user.Email)
            {
                user.Email = model.Email;
                // If you use Email as Username, update it here too:
                // user.Username = model.Email; 
            }

            // ==========================================
            // SAVE CHANGES
            // ==========================================
            try
            {
                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                // OPTIONAL: Refresh the cookie claims if specific details (like Name/Role) changed
                // await SignInUser(user); 

                return Json(new { success = true, message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file received." });
            }
            var imageValidation = Helper.ValidateImage(file);
            if (imageValidation != ValidationResult.Success)
            {
                return Json(new { success = false, message = imageValidation.ErrorMessage });
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            try
            {
                if (!string.IsNullOrEmpty(user.ImageURL) && user.ImageURL != "default-profile.png")
                {
                    string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles", user.ImageURL);
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }
                string newFileName = Helper.SaveImage(file, "profiles", _webHostEnvironment.WebRootPath);
                user.ImageURL = newFileName;
                _db.Users.Update(user);
                await _db.SaveChangesAsync();
                await SignInUser(user);
                return Json(new { success = true, message = "Photo updated!", newUrl = newFileName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving image: " + ex.Message });
            }
        }
        // =======================================================
        // CHANGE PASSWORD (LOGGED IN USER)
        // =======================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Validation failed.", errors = errors });
            }

            // 1. Get Current User
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _db.Users.FindAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User session invalid. Please re-login." });
            }

            // 2. Verify Old Password
            bool isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash);
            if (!isOldPasswordCorrect)
            {
                return Json(new { success = false, message = "The current password you entered is incorrect." });
            }

            // =========================================================================
            // NEW CHECK: Prevent New Password from being the same as Current Password
            // =========================================================================
            if (model.CurrentPassword == model.NewPassword)
            {
                return Json(new { success = false, message = "New password cannot be the same as your current password." });
            }

            // 3. Update to New Password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully!" });
        }
        // =======================================================
        // DELETE ACCOUNT (SOFT DELETE)
        // =======================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false });

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return Json(new { success = false });

            try
            {
                // --- THE SOFT DELETE SWITCH ---
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.Status = LMSUser.UserStatus.INACTIVE; // Optional: keep as INACTIVE too

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
    }
