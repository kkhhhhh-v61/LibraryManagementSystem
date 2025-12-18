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


        // Calender
        //[Authorize(Roles = "ADMIN")]
        public IActionResult MasterCalendar()
        {
            ViewBag.Categories = db.RoomCategories.ToList();
            // 页面入口
            return View();
        }

        // 1. 获取左侧的“资源” (即房间列表)
        //[Authorize(Roles = "ADMIN")]
        [HttpGet]
        public IActionResult GetResources(string? categoryId)
        {
            var query = db.Rooms
                .Include(r => r.RoomCategory)
                .AsQueryable();

            // ★ 新增逻辑：如果传了 categoryId，就只查这个分类下的房间
            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(r => r.RoomCategoryId == categoryId);
            }

            var rooms = query
                .OrderBy(r => r.RoomCategoryId)
                .ThenBy(r => r.Name)
                .Select(r => new
                {
                    id = r.Id,
                    title = r.Name,
                    // 扩展属性，用于前端变灰处理
                    extendedProps = new { status = r.Status.ToString() }
                })
                .ToList();

            return Json(rooms);
        }

        // 2. 获取预订数据 (必须包含 resourceId)
        //[Authorize(Roles = "ADMIN")]
        [HttpGet]
        public IActionResult GetCalendarData(string? categoryId)
        {
            var query = db.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                // 注意：BookingStatus 也是 Enum，所以这里用 != 是对的
                .Where(b => b.Status != Booking.BookingStatus.CANCELLED)
                .AsQueryable();

            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(b => b.Room.RoomCategoryId == categoryId);
            }

            var bookings = query.ToList();

            var events = bookings.Select(b => {

                // ★ 核心修正：直接对比 Enum
                // 不再需要 string.Equals，也不需要 Trim() 或 ToUpper()
                bool isMaintenance = (b.Room.Status == Room.RoomStatus.MAINTENANCE);

                return new
                {
                    id = b.Id,
                    resourceId = b.RoomId,
                    title = b.User.Username,
                    start = b.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = b.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),

                    // 颜色逻辑
                    backgroundColor = isMaintenance
                                      ? "#94a3b8" // 灰色
                                      : (b.Status == Booking.BookingStatus.PENDING ? "#f59e0b" : "#10b981"),

                    borderColor = isMaintenance
                                  ? "#94a3b8"
                                  : (b.Status == Booking.BookingStatus.PENDING ? "#f59e0b" : "#10b981"),

                    textColor = "#ffffff",

                    extendedProps = new
                    {
                        roomName = b.Room.Name,
                        userName = b.User.Username,
                        price = b.TotalPrice,
                        // 显示状态文本
                        status = isMaintenance ? "MAINTENANCE" : b.Status.ToString()
                    }
                };
            }).ToList();

            return Json(events);
        }
    }
}