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
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            if (ModelState.IsValid)
            {
                // 1. Check for duplicates
                if (_db.Users.Any(u => u.Username == model.Username || u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Username or Email already exists.");
                    return View(model);
                }

                // 2. Generate a Sequential ID (e.g., S00002)
                var lastStaff = _db.Users
                    .Where(u => u.Id.StartsWith("S"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefault();

                string newId;
                if (lastStaff == null)
                {
                    newId = "S00001"; // Start here if no staff exist
                }
                else
                {
                    // Extract the number part, increment it, and format back to 5 digits
                    int lastIdNumber = int.Parse(lastStaff.Id.Substring(1));
                    newId = "S" + (lastIdNumber + 1).ToString("D5");
                }

                // 3. Map View Model to Database Model
                var newStaff = new User
                {
                    Id = newId,
                    Username = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    ContactNo = model.ContactNo,
                    PasswordHash = hashedPassword, // Note: In production, use hashing!
                    Role = UserRole.STAFF,
                    Status = UserStatus.ACTIVE,
                    CredibilityScore = 100,
                    FailedLoginAttempts = 0,
                    IsProfilePublic = true
                };

                // 4. Save to Database
                _db.Users.Add(newStaff);
                await _db.SaveChangesAsync();

                TempData["Success"] = "New Staff Member " + newId + " Created Successfully!";
                return RedirectToAction("Index", "Staff"); // Redirect to the working UI list
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
                Username = user.Username, // Read-only usually
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ContactNo = user.ContactNo,
                Role = user.Role.ToString(), // Convert Enum to String
                CredibilityScore = user.CredibilityScore,
                IsProfilePublic = user.IsProfilePublic
                // We don't map Password here (security)
            };

            ViewData["UserId"] = user.Id; // Keep ID separate for the form post
            return View(model);
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
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.ContactNo = model.ContactNo;
                user.CredibilityScore = model.CredibilityScore;

                _db.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await _db.SaveChangesAsync();

                TempData["ToastMessage"] = "Update Successful!";
                TempData["ToastType"] = "success";
                return RedirectToAction("Index", "Staff");
            }

            // DEBUG: If you reach here, the button "did nothing" because validation failed.
            // This will print the errors to your Output window in Visual Studio
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors) { System.Diagnostics.Debug.WriteLine(error.ErrorMessage); }

            ViewData["UserId"] = id;
            return View(model);
        }
    }
}