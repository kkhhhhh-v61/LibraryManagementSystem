using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static LibraryManagementSystem.Models.Booking;
using static LibraryManagementSystem.Models.Payment;
using static LibraryManagementSystem.Models.Room;

namespace LibraryManagementSystem.Controllers
{
    [Authorize]
    public class BookingController(LMSDB db) : Controller
    {
        // GET: Booking Page
        [Authorize(Roles = "MEMBER")]
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
            else if ((reqEnd - reqStart).TotalMinutes < 30)
            {
                ModelState.AddModelError("EndTime", "Booking duration must be at least 30 minutes.");
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

                var nextFreeSlot = db.Bookings
        .Include(b => b.Room)
        .Where(b => b.Room.RoomCategoryId == vm.RoomCategoryId)
        .Where(b => b.Status != BookingStatus.CANCELLED)
        .Where(b => b.Status != BookingStatus.COMPLETED)
        .Where(b => b.EndTime > reqStart)
        .OrderBy(b => b.EndTime)
        .Select(b => b.EndTime)
        .FirstOrDefault();

                string errorMsg = "No rooms available for this specific time slot.";

                if (nextFreeSlot != DateTime.MinValue)
                {
                    string timeStr = nextFreeSlot.ToString("h:mm tt");
                    errorMsg += $" All rooms are currently occupied. The next room becomes available after {timeStr}.";
                }
                else
                {
                    errorMsg += " Rooms might be under maintenance.";
                }

                TempData["Error"] = errorMsg;
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

            return RedirectToAction("Index", "Payment", new { bookingId = newBooking.Id });
        }

        [Authorize(Roles = "MEMBER")]
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

        [Authorize(Roles = "MEMBER")]
        public IActionResult BookingHistory(string search = "", string date = "")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var query = db.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomCategory)
                .Where(b => b.UserId == userId);

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out DateTime selectedDate))
            {
                query = query.Where(b => b.StartTime.Date == selectedDate.Date);
            }

            var list = query.OrderByDescending(b => b.StartTime).ToList();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();

                list = list.Where(b =>
                    b.Room.Name.ToLower().Contains(search) ||
                    b.Room.RoomCategory.Name.ToLower().Contains(search) ||
                    b.TotalPrice.ToString("F2").Contains(search) ||
                    b.StartTime.ToString("hh:mm tt").ToLower().Contains(search) ||
                    b.EndTime.ToString("hh:mm tt").ToLower().Contains(search)
                ).ToList();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_BookingList", list);
            }

            return View(list);
        }

        // POST: Cancel / Refund Booking
        [HttpPost]
        [Authorize(Roles = "MEMBER")]
        public IActionResult Cancel(string id, string? reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = db.Bookings.FirstOrDefault(b => b.Id == id && b.UserId == userId);

            if (booking == null || booking.Status == BookingStatus.COMPLETED || booking.Status == BookingStatus.CANCELLED)
            {
                return RedirectToAction("BookingHistory");
            }

            string cleanReason = reason?.Trim() ?? "";

            if (booking.Status == BookingStatus.CONFIRMED)
            {
                if (string.IsNullOrEmpty(cleanReason))
                {
                    TempData["Error"] = "Please provide a reason for the refund.";
                    return RedirectToAction("BookingHistory");
                }

                if (cleanReason.Length < 5)
                {
                    TempData["Error"] = "Reason is too short. Please provide at least 5 characters.";
                    return RedirectToAction("BookingHistory");
                }

            }

            if (cleanReason.Length > 250)
            {
                TempData["Error"] = "Reason is too long. Max 250 characters allowed.";
                return RedirectToAction("BookingHistory");
            }

            if (booking.Status == BookingStatus.PENDING)
            {
                booking.Status = BookingStatus.CANCELLED;
                booking.CancelledAt = DateTime.Now;
                booking.CancellationReason = string.IsNullOrEmpty(cleanReason) ? "User Cancelled" : cleanReason;

                db.SaveChanges();
                TempData["Success"] = "Booking cancelled successfully.";
            }
            else if (booking.Status == BookingStatus.CONFIRMED)
            {
                var payment = db.Payments
                    .OrderByDescending(p => p.Date)
                    .FirstOrDefault(p => p.BookingId == booking.Id && p.Status == PaymentStatus.SUCCESS);

                if (payment != null)
                {
                    var timeSincePayment = DateTime.Now - payment.Date;
                    if (timeSincePayment.TotalMinutes > 30)
                    {
                        TempData["Error"] = "Refund Failed: Refunds are only available within 30 minutes of payment.";
                        return RedirectToAction("BookingHistory");
                    }
                }

                booking.Status = BookingStatus.CANCELLED;
                booking.CancelledAt = DateTime.Now;
                booking.CancellationReason = cleanReason;

                db.SaveChanges();
                TempData["Success"] = "Refund request submitted successfully.";
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
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult MasterCalendar()
        {
            ViewBag.Categories = db.RoomCategories.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult GetResources(string? categoryId)
        {
            var query = db.Rooms
                .Include(r => r.RoomCategory)
                .AsQueryable();

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
                    extendedProps = new { status = r.Status.ToString() }
                })
                .ToList();

            return Json(rooms);
        }

        [HttpGet]
        public IActionResult GetCalendarData(string? categoryId)
        {
            var expiredBookings = db.Bookings
                .Where(b => b.Status == BookingStatus.CONFIRMED && b.EndTime <= DateTime.Now)
                .ToList();

            if (expiredBookings.Any())
            {
                foreach (var b in expiredBookings)
                {
                    b.Status = BookingStatus.COMPLETED;
                }
                db.SaveChanges();
            }

            var query = db.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Where(b => b.Status != BookingStatus.CANCELLED && b.Status != BookingStatus.COMPLETED)
                .AsQueryable();

            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(b => b.Room.RoomCategoryId == categoryId);
            }

            var bookings = query.ToList();

            var events = bookings.Select(b => {

                bool isMaintenance = (b.Room.Status == RoomStatus.MAINTENANCE);

                string colorCode = isMaintenance
                                   ? "#94a3b8"
                                   : (b.Status == BookingStatus.PENDING ? "#f59e0b" : "#3b82f6");

                return new
                {
                    id = b.Id,
                    resourceId = b.RoomId,
                    title = b.User.Username,
                    start = b.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = b.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),

                    backgroundColor = colorCode,
                    borderColor = colorCode,
                    textColor = "#ffffff",

                    extendedProps = new
                    {
                        roomName = b.Room.Name,
                        userName = b.User.Username,
                        price = b.TotalPrice,
                        status = isMaintenance ? "MAINTENANCE" : b.Status.ToString(),
                        description = b.Description ?? ""
                    }
                };
            }).ToList();

            return Json(events);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult UpdateDescription(string id, string description)
        {
            var booking = db.Bookings.Find(id);
            if (booking == null)
            {
                return Json(new { success = false, message = "Booking not found" });
            }

            booking.Description = description;
            db.SaveChanges();

            return Json(new { success = true, message = "Description updated successfully" });
        }

        // POST: Move Booking (Drag & Drop)
        [HttpPost]
        [Authorize(Roles = "ADMIN, STAFF")]
        public IActionResult MoveBooking(string id, string newRoomId)
        {
            var strategy = db.Database.CreateExecutionStrategy();

            return strategy.Execute(() =>
            {
                using var transaction = db.Database.BeginTransaction();

                try
                {
                    var sourceBooking = db.Bookings
                        .Include(b => b.Room)
                        .FirstOrDefault(b => b.Id == id);

                    if (sourceBooking == null) return Json(new { success = false, message = "Source booking not found." });

                    if (sourceBooking.Status == BookingStatus.COMPLETED || sourceBooking.Status == BookingStatus.CANCELLED)
                    {
                        return Json(new { success = false, message = "Cannot move completed or cancelled bookings." });
                    }

                    var targetRoom = db.Rooms.Find(newRoomId);
                    if (targetRoom == null) return Json(new { success = false, message = "Target room not found." });

                    if (targetRoom.Status == RoomStatus.MAINTENANCE)
                    {
                        return Json(new { success = false, message = "Target room is under maintenance." });
                    }

                    var conflictingBooking = db.Bookings
                        .Include(b => b.Room)
                        .FirstOrDefault(b =>
                            b.RoomId == newRoomId &&
                            b.Status != BookingStatus.CANCELLED &&
                            sourceBooking.StartTime < b.EndTime &&
                            sourceBooking.EndTime > b.StartTime
                        );

                    if (conflictingBooking == null)
                    {
                        sourceBooking.RoomId = newRoomId;
                        db.SaveChanges();
                        transaction.Commit();
                        return Json(new { success = true, message = "Booking moved successfully!", action = "move" });
                    }
                    else
                    {
                        if (sourceBooking.Room.RoomCategoryId != targetRoom.RoomCategoryId)
                        {
                            return Json(new { success = false, message = "Cannot swap: Different room categories." });
                        }

                        bool isSourceRoomBlocked = db.Bookings.Any(b =>
                            b.RoomId == sourceBooking.RoomId &&
                            b.Id != sourceBooking.Id &&
                            b.Status != BookingStatus.CANCELLED &&
                            conflictingBooking.StartTime < b.EndTime &&
                            conflictingBooking.EndTime > b.StartTime
                        );

                        if (isSourceRoomBlocked)
                        {
                            return Json(new { success = false, message = "Cannot swap: Source room has overlap with other bookings." });
                        }

                        string sourceRoomId = sourceBooking.RoomId;

                        sourceBooking.RoomId = newRoomId;
                        conflictingBooking.RoomId = sourceRoomId;

                        db.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, message = "Bookings swapped successfully!", action = "swap" });
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "System Error: " + ex.Message });
                }
            });
        }
    }
}