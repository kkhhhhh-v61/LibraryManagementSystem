using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;       
using Microsoft.EntityFrameworkCore;  
using LibraryManagementSystem.Models;
using Microsoft.Extensions.Configuration; 

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "STAFF,ADMIN")]
    public class StaffController : Controller
    {
        private readonly LMSDB _db;
        private readonly IConfiguration _configuration;

        public StaffController(LMSDB db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int pageSize = 5;
            var users = _db.Users.OrderBy(u => u.Id).Take(pageSize).ToList();
            int totalRecords = _db.Users.Count();
            ViewBag.TotalRecords = totalRecords;
            ViewBag.CurrentPage = 1;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.HasPrevious = false;
            ViewBag.HasNext = totalRecords > pageSize;

            return View(users); 
        }
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
            var query = _db.Users.AsQueryable();
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

            // SORTING LOGIC 
            query = sortOrder switch
            {
                "id_desc" => query.OrderByDescending(u => u.Id),
                "name" => query.OrderBy(u => u.Username),
                "name_desc" => query.OrderByDescending(u => u.Username),
                "role_asc" => query.OrderBy(u => u.Role), 
                "role_desc" => query.OrderByDescending(u => u.Role),
                "role" => query.OrderBy(u => u.Role),
                _ => query.OrderBy(u => u.Id),
            };
            int pageSize = 5;
            int totalRecords = query.Count();
            int currentPage = pageNumber ?? 1;
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            var userList = query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPrevious = currentPage > 1;
            ViewBag.HasNext = currentPage < totalPages;
            ViewBag.TotalRecords = totalRecords;

            return PartialView("_UserTableBody", userList);
        }
    }
}