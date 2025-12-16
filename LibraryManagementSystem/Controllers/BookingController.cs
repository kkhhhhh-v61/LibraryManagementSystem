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
            // 1. 默认日期
            DateTime selectedDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);

            // 2. 加载所有 Category (用于顶部导航)
            var allCategories = db.RoomCategories.OrderBy(c => c.Id).ToList();

            // 3. 确定当前 Category (默认取第一个)
            string activeCatId = categoryId ?? allCategories.FirstOrDefault()?.Id ?? "";

            // 4. 构建 VM
            var vm = new BookingIndexVM
            {
                BookingDate = selectedDate,
                CurrentCategoryId = activeCatId,
                Categories = allCategories,

                // 默认 9:00 - 11:00
                StartTime = selectedDate.Date.AddHours(9),
                EndTime = selectedDate.Date.AddHours(11),

                // 以下列表设为空，因为我们不再用 Timeline 了
                CurrentCategoryRooms = new List<Room>(),
                DailyBookings = new Dictionary<string, List<Booking>>()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(BookingIndexVM vm)
        {
            // 这里处理提交逻辑 (System Auto-Assign)
            // 暂时重定向回 Index
            return RedirectToAction("Index");
        }
    }
}