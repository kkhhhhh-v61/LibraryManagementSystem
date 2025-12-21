using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static LibraryManagementSystem.Models.User; // For Enum access

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminController : Controller
    {
        private readonly LMSDB _db;

        public AdminController(LMSDB db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Staff");
        }
        // 2. CREATE STAFF (GET)
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                // 1. IMPROVED: Individual checks for clear feedback
                bool isUsernameTaken = _db.Users.Any(u => u.Username == model.Username);
                bool isEmailTaken = _db.Users.Any(u => u.Email == model.Email);
                bool isPhoneTaken = _db.Users.Any(u => u.ContactNo == model.ContactNo);

                if (isUsernameTaken || isEmailTaken || isPhoneTaken)
                {
                    if (isUsernameTaken)
                        ModelState.AddModelError("Username", "This username is already registered.");

                    if (isEmailTaken)
                        ModelState.AddModelError("Email", "This email is already in use.");

                    if (isPhoneTaken)
                        ModelState.AddModelError("ContactNo", "This phone number is already registered.");

                    // Add a toast so they know WHY the button click didn't redirect
                    TempData["ToastMessage"] = "Please correct the highlighted errors.";
                    TempData["ToastType"] = "error";

                    return View(model);
                }

                // 2. Security: Hash password only AFTER validation passes
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // 3. Generate Sequential ID
                var lastStaff = _db.Users
                    .Where(u => u.Id.StartsWith("S"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefault();

                string newId = lastStaff == null
                    ? "S00001"
                    : "S" + (int.Parse(lastStaff.Id.Substring(1)) + 1).ToString("D5");

                // 4. Mapping
                var newStaff = new User
                {
                    Id = newId,
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    ContactNo = model.ContactNo,
                    PasswordHash = hashedPassword,
                    Role = UserRole.STAFF,
                    Status = UserStatus.ACTIVE,
                    CredibilityScore = 100,
                    FailedLoginAttempts = 0,
                    IsProfilePublic = true
                };

                _db.Users.Add(newStaff);
                await _db.SaveChangesAsync();

                TempData["ToastMessage"] = "Staff member successfully registered!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Staff");
            }

            return View(model);
        }

        // 4. LOCK / UNLOCK USER
        [HttpPost] // Make sure your button is inside a <form> with method="post"
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                // Toggle the status
                user.Status = (user.Status == UserStatus.ACTIVE)
                              ? UserStatus.INACTIVE
                              : UserStatus.ACTIVE;

                await _db.SaveChangesAsync();
            }
            // Refresh the page so you see the new status immediately
            return RedirectToAction("Index");
        }
        // GET: Admin/Edit/U00001
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Map Database User -> ProfileVM
            var model = new LibraryManagementSystem.Models.ProfileVM
            {
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ContactNo = user.ContactNo,
                Role = user.Role.ToString(),
                CredibilityScore = user.CredibilityScore,
                IsProfilePublic = user.IsProfilePublic
            };

            // REMOVE THE REDIRECT AND TOAST MESSAGE FROM HERE
            // These belonged in the Create method, not the Edit GET method.

            ViewData["UserId"] = id; // Ensure the ID is passed to the view for the form
            return View(model);      // This allows the Edit page to actually load!
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, LibraryManagementSystem.Models.ProfileVM model)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Remove validation for fields that the Admin isn't changing or aren't in the form
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (ModelState.IsValid)
            {
                // 1. Update the user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.ContactNo = model.ContactNo;
                user.CredibilityScore = model.CredibilityScore;

                _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await _db.SaveChangesAsync();

                // 2. Set the Success Toast Message here!
                TempData["ToastMessage"] = "Profile for " + user.Username + " updated successfully!";
                TempData["ToastType"] = "success";

                // 3. Redirect back to the list
                return RedirectToAction("Index", "Staff");
            }

            // If validation fails, stay on the edit page
            ViewData["UserId"] = id;
            return View(model);
        }
    }
}