using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static LibraryManagementSystem.Models.Booking;
using static LibraryManagementSystem.Models.Room;

namespace LibraryManagementSystem.Controllers
{
    public class BookingController(LMSDB db) : Controller
    {
        public IActionResult Index(string? categoryId, string? date)
        {
            DateTime selectedDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);

            var allCategories = db.RoomCategories.OrderBy(c => c.Id).ToList();

            string activeCatId = categoryId ?? allCategories.FirstOrDefault()?.Id ?? "";

            var vm = new BookingIndexVM
            {
                BookingDate = selectedDate,
                CurrentCategoryId = activeCatId,
                Categories = allCategories,

                StartTime = selectedDate.Date.AddHours(9),
                EndTime = selectedDate.Date.AddHours(11),

                CurrentCategoryRooms = new List<Room>(),
                DailyBookings = new Dictionary<string, List<Booking>>()
            };

            if (!string.IsNullOrEmpty(activeCatId))
            {
                vm.CurrentCategoryRooms = db.Rooms
                    .Where(r => r.RoomCategoryId == activeCatId)
                    .OrderBy(r => r.Id)
                    .ToList();
            }

            var dayStart = selectedDate.Date;
            var dayEnd = dayStart.AddDays(1);

            var roomIds = vm.CurrentCategoryRooms.Select(r => r.Id).ToList();

            var dayBookings = db.Bookings
                .Include(b => b.User) 
                .Where(b => b.StartTime < dayEnd && b.EndTime > dayStart)
                .Where(b => b.Status != BookingStatus.CANCELLED)
                .Where(b => roomIds.Contains(b.RoomId))
                .ToList();

            vm.DailyBookings = dayBookings
                .GroupBy(b => b.RoomId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(BookingIndexVM vm)
        {
            return RedirectToAction("Index");
        }
    }
}