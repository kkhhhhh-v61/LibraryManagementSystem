using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;       // Needed for ADO.NET
using Microsoft.EntityFrameworkCore;  // Needed for EF Core (UserDetails)
using LibraryManagementSystem.Models; // Ensure this matches your namespace
using Microsoft.Extensions.Configuration; // Needed for IConfiguration

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "STAFF,ADMIN")]
    public class StaffController : Controller
    {
        private readonly LMSDB _db;
        private readonly IConfiguration _configuration;

        // --- FIX 1: Add 'IConfiguration configuration' to the constructor ---
        public StaffController(LMSDB db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration; // Assign it here!
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Fetch the first page of users immediately so the view isn't empty
            int pageSize = 5;
            var users = _db.Users.OrderBy(u => u.Id).Take(pageSize).ToList();

            // Set initial metadata for the first load
            int totalRecords = _db.Users.Count();
            ViewBag.TotalRecords = totalRecords;
            ViewBag.CurrentPage = 1;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.HasPrevious = false;
            ViewBag.HasNext = totalRecords > pageSize;

            return View(users); // Pass the list instead of empty List<User>()
        }

        // GET: Staff/UserDetails/{id}
        // (This uses EF Core, which is fine if your lecturer allows mixed methods)
        public async Task<IActionResult> UserDetails(string id)
        {
            if (id == null) return NotFound();

            var user = await _db.Users
                .Include(u => u.Loans)
                    .ThenInclude(l => l.Copy)
                        .ThenInclude(c => c.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpGet]
        public IActionResult FilterUsers(string searchString, string sortOrder, int? pageNumber)
        {
            // 1. Start with the existing EF Context (Safe & Reliable)
            var query = _db.Users.AsQueryable();

            // 2. Search Logic (Case Insensitive)
            if (!string.IsNullOrEmpty(searchString))
            {
                string lowerSearch = searchString.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(lowerSearch) ||
                    u.Email.ToLower().Contains(lowerSearch) ||
                    u.Id.ToLower().Contains(lowerSearch) ||
                    (u.ContactNo != null && u.ContactNo.Contains(lowerSearch))
                );
            }

            // --- 3. SORTING LOGIC ---
            query = sortOrder switch
            {
                "id_desc" => query.OrderByDescending(u => u.Id),

                // CHANGE THIS LINE: from "name_asc" to "name"
                "name" => query.OrderBy(u => u.Username),

                // This one is already correct
                "name_desc" => query.OrderByDescending(u => u.Username),

                "role_asc" => query.OrderBy(u => u.Role), // Note: Check if JS sends "role" or "role_asc"
                "role_desc" => query.OrderByDescending(u => u.Role),

                // Check your JS for "role" too. If JS sends "role", add this case:
                "role" => query.OrderBy(u => u.Role),

                _ => query.OrderBy(u => u.Id), // Default
            };

            // 4. Paging Logic
            int pageSize = 5;
            int totalRecords = query.Count();
            int currentPage = pageNumber ?? 1;
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var userList = query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 5. Pass Metadata to ViewBag
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPrevious = currentPage > 1;
            ViewBag.HasNext = currentPage < totalPages;
            ViewBag.TotalRecords = totalRecords;

            return PartialView("_UserTableBody", userList);
        }
    }
}