using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;
using static LibraryManagementSystem.Models.User; 
using System.Threading.Tasks;
using System.Linq;
using System;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "ADMIN, STAFF, Admin, Staff")]
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

        // ==========================================================
        // 2. CREATE STAFF
        // ==========================================================
        [Authorize(Roles = "ADMIN,Admin")]
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View();
        }

        [Authorize(Roles = "ADMIN,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(RegisterVM model)
        {
            if (ModelState.IsValid)
            {
                bool isUsernameTaken = _db.Users.Any(u => u.Username == model.Username);
                bool isEmailTaken = _db.Users.Any(u => u.Email == model.Email);
                bool isPhoneTaken = _db.Users.Any(u => u.ContactNo == model.ContactNo);

                if (isUsernameTaken || isEmailTaken || isPhoneTaken)
                {
                    if (isUsernameTaken) ModelState.AddModelError("Username", "This username is already registered.");
                    if (isEmailTaken) ModelState.AddModelError("Email", "This email is already in use.");
                    if (isPhoneTaken) ModelState.AddModelError("ContactNo", "This phone number is already registered.");

                    TempData["ToastMessage"] = "Please correct the highlighted errors.";
                    TempData["ToastType"] = "error";
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                var lastStaff = _db.Users
                    .Where(u => u.Id.StartsWith("S"))
                    .OrderByDescending(u => u.Id)
                    .FirstOrDefault();

                string newId = lastStaff == null
                    ? "S00001"
                    : "S" + (int.Parse(lastStaff.Id.Substring(1)) + 1).ToString("D5");

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

        // ==========================================================
        // 4. LOCK / UNLOCK USER
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                user.Status = (user.Status == UserStatus.ACTIVE)
                              ? UserStatus.INACTIVE
                              : UserStatus.ACTIVE;

                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Staff");
        }

        // ==========================================================
        // EDIT USER (GET)
        // ==========================================================
        [Authorize(Roles = "ADMIN,Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            var model = new ProfileVM
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

            ViewData["UserId"] = id;
            return View(model);
        }

        // ==========================================================
        // 5. ACTIVITY LOG (History Dashboard) - SORTING, AJAX & PAGINATION
        // ==========================================================
        [Authorize(Roles = "Admin, Staff")]
        [HttpGet]
        public async Task<IActionResult> ActivityLog(string searchString, string sortOrder, int pageNumber = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IdSortParam"] = string.IsNullOrEmpty(sortOrder) ? "id_asc" : "";
            ViewData["UserSortParam"] = sortOrder == "user" ? "user_desc" : "user";
            ViewData["DateSortParam"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["StatusSortParam"] = sortOrder == "status" ? "status_desc" : "status";
            var model = await GetActivityLogData(searchString, sortOrder, pageNumber);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ActivityLogTables", model);
            }
            return View(model);
        }
        private async Task<ActivityLogVM> GetActivityLogData(string searchTerm, string sortOrder, int page)
        {
            int pageSize = 5; 
            var model = new ActivityLogVM();

            // PREPARE QUERIES 
            var loansQuery = _db.Loans
                .Include(l => l.User)
                .Include(l => l.Copy).ThenInclude(c => c.Book)
                .AsQueryable();

            var bookingsQuery = _db.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                .AsQueryable();

            // APPLY SEARCH 
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                // Filter Loans
                loansQuery = loansQuery.Where(l =>
                    l.User.Username.ToLower().Contains(searchTerm) ||
                    l.Copy.Book.Title.ToLower().Contains(searchTerm) ||
                    l.Id.Contains(searchTerm));

                // Filter Bookings
                bookingsQuery = bookingsQuery.Where(b =>
                    b.User.Username.ToLower().Contains(searchTerm) ||
                    b.Room.Name.ToLower().Contains(searchTerm) ||
                    b.Id.ToString().Contains(searchTerm));
            }

            // APPLY SORTING
            switch (sortOrder)
            {
                case "id_asc":
                    loansQuery = loansQuery.OrderBy(l => l.Id);
                    bookingsQuery = bookingsQuery.OrderBy(b => b.Id);
                    break;

                case "user":
                    loansQuery = loansQuery.OrderBy(l => l.User.Username);
                    bookingsQuery = bookingsQuery.OrderBy(b => b.User.Username);
                    break;
                case "user_desc":
                    loansQuery = loansQuery.OrderByDescending(l => l.User.Username);
                    bookingsQuery = bookingsQuery.OrderByDescending(b => b.User.Username);
                    break;

                case "date":
                    loansQuery = loansQuery.OrderBy(l => l.LoanDate);
                    bookingsQuery = bookingsQuery.OrderBy(b => b.BookingDate);
                    break;
                case "date_desc":
                    loansQuery = loansQuery.OrderByDescending(l => l.LoanDate);
                    bookingsQuery = bookingsQuery.OrderByDescending(b => b.BookingDate);
                    break;

                case "status":
                    loansQuery = loansQuery.OrderBy(l => l.Status);
                    bookingsQuery = bookingsQuery.OrderBy(b => b.Status);
                    break;
                case "status_desc":
                    loansQuery = loansQuery.OrderByDescending(l => l.Status);
                    bookingsQuery = bookingsQuery.OrderByDescending(b => b.Status);
                    break;

                default: // Default: Newest ID first
                    loansQuery = loansQuery.OrderByDescending(l => l.Id);
                    bookingsQuery = bookingsQuery.OrderByDescending(b => b.Id);
                    break;
            }

            // PAGINATION LOGIC
            int totalLoans = await loansQuery.CountAsync();
            int totalBookings = await bookingsQuery.CountAsync();
            int maxItems = Math.Max(totalLoans, totalBookings);
            model.TotalPages = (int)Math.Ceiling(maxItems / (double)pageSize);
            model.CurrentPage = page;
            if (model.CurrentPage < 1) model.CurrentPage = 1;
            if (model.TotalPages > 0 && model.CurrentPage > model.TotalPages) model.CurrentPage = model.TotalPages;
            int skipAmount = (model.CurrentPage - 1) * pageSize;
            model.Loans = await loansQuery
                .Skip(skipAmount)
                .Take(pageSize)
                .Select(l => new AdminLoanDTO
                {
                    LoanId = l.Id,
                    BookTitle = l.Copy.Book.Title,
                    BookImageUrl = l.Copy.Book.ImageURL,
                    UserId = l.User.Id,
                    UserName = l.User.Username,
                    UserEmail = l.User.Email,
                    UserProfileUrl = l.User.ImageURL,
                    LoanDate = l.LoanDate,
                    LoanDue = l.LoanDue,
                    ReturnDate = l.ReturnDate,
                    Status = l.Status.ToString()
                }).ToListAsync();

            model.Bookings = await bookingsQuery
                .Skip(skipAmount)
                .Take(pageSize)
                .Select(b => new AdminBookingDTO
                {
                    BookingId = b.Id,
                    FacilityName = b.Room.Name,
                    UserId = b.User.Id,
                    UserName = b.User.Username,
                    UserProfileUrl = b.User.ImageURL,
                    BookingDate = b.BookingDate,
                    StartTime = b.StartTime.TimeOfDay,
                    EndTime = b.EndTime.TimeOfDay,
                    Status = b.Status.ToString()
                }).ToListAsync();

            return model;
        }
    }
}