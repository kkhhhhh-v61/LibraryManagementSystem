using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static LibraryManagementSystem.Models.Booking;
using static LibraryManagementSystem.Models.Room;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "MEMBER")]
    public class BookingController(LMSDB db) : Controller
    {
        // GET: Booking Page
        public IActionResult Index(string? roomCategoryId, string? date)
        {
            DateTime targetDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
            if (targetDate < DateTime.Today) targetDate = DateTime.Today;

            var allCategories = db.RoomCategories.OrderBy(c => c.Id).ToList();
            string activeCatId = roomCategoryId ?? allCategories.FirstOrDefault()?.Id ?? "";

            ViewBag.Categories = allCategories;

            var now = DateTime.Now;
            int nextHour = now.Hour + 1;
            if (nextHour < 9) nextHour = 9;
            if (nextHour >= 19) nextHour = 9;

            var vm = new BookingCreateVM
            {
                BookingDate = targetDate,
                RoomCategoryId = activeCatId,

                StartTime = new TimeSpan(nextHour, 0, 0),
                EndTime = new TimeSpan(nextHour + 2, 0, 0),

                CategoryList = allCategories.Select(c => new SelectListItem
                {
                    Value = c.Id,
                    Text = c.Name
                }).ToList()
            };

            return View(vm);
        }

        // POST: Create Booking
        [HttpPost]
        [Authorize(Roles = "MEMBER")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookingCreateVM vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            DateTime reqStart = vm.BookingDate.Date + vm.StartTime;
            DateTime reqEnd = vm.BookingDate.Date + vm.EndTime;

            if (reqStart < DateTime.Now)
            {
                ModelState.AddModelError("StartTime", "Start time must be in the future.");
            }

            if (reqEnd <= reqStart)
            {
                ModelState.AddModelError("EndTime", "End time must be after start time.");
            }

            if (!ModelState.IsValid)
            {
                ReloadViewData(vm);
                TempData["Error"] = "Please check your time selection.";
                return View("Index", vm);
            }

            var occupiedRoomIds = db.Bookings
                .Where(b => b.Status != BookingStatus.CANCELLED) 
                .Where(b => reqStart < b.EndTime && b.StartTime < reqEnd) 
                .Select(b => b.RoomId);

            var availableRoom = db.Rooms
                .Where(r => r.RoomCategoryId == vm.RoomCategoryId)
                .Where(r => r.Status == RoomStatus.AVAILABLE)
                .Where(r => !occupiedRoomIds.Contains(r.Id))
                .FirstOrDefault();

            if (availableRoom == null)
            {

                TempData["Error"] = "No rooms available for this specific time slot.";
                ReloadViewData(vm);
                return View("Index", vm);
            }

            var category = db.RoomCategories.Find(vm.RoomCategoryId);
            decimal hours = (decimal)(reqEnd - reqStart).TotalHours;
            decimal totalPrice = hours * category.HourlyRate;

            var newBooking = new Booking
            {
                Id = GenerateBookingId(),
                RoomId = availableRoom.Id,
                UserId = userId,

                BookingDate = DateTime.Now,

                StartTime = reqStart,
                EndTime = reqEnd,

                TotalPrice = totalPrice,
                Status = BookingStatus.PENDING
            };

            db.Bookings.Add(newBooking);
            db.SaveChanges();

            TempData["Success"] = "Booking created successfully!";
            return RedirectToAction("Index");
        }

        private void ReloadViewData(BookingCreateVM vm)
        {
            var allCategories = db.RoomCategories.OrderBy(c => c.Id).ToList();
            ViewBag.Categories = allCategories;
            vm.CategoryList = allCategories.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = c.Name
            }).ToList();
        }

        public IActionResult BookingHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = db.Bookings.Include(b => b.Room).ThenInclude(r => r.RoomCategory)
                         .Where(b => b.UserId == userId).OrderByDescending(b => b.BookingDate).ToList();
            return View(list);
        }

        [HttpPost]
        public IActionResult Cancel(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (booking != null && booking.Status != BookingStatus.COMPLETED && booking.Status != BookingStatus.CANCELLED)
            {
                booking.Status = BookingStatus.CANCELLED;
                booking.CancelledAt = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = "Booking cancelled.";
            }
            return RedirectToAction("BookingHistory");
        }

        private string GenerateBookingId()
        {
            var lastId = db.Bookings.OrderByDescending(b => b.Id).Select(b => b.Id).FirstOrDefault();
            if (string.IsNullOrEmpty(lastId)) return "B00001";
            if (lastId.Length >= 6 && int.TryParse(lastId.Substring(1), out int num)) return "B" + (num + 1).ToString("D5");
            return "B" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        }
    }
}