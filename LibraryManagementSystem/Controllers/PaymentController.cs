using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static LibraryManagementSystem.Models.Booking;
using static LibraryManagementSystem.Models.Payment;
using static LibraryManagementSystem.Models.Room;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = "MEMBER")]
    public class PaymentController(LMSDB db) : Controller
    {
        [HttpGet]
        public IActionResult Index(string bookingId)
        {
            if (string.IsNullOrEmpty(bookingId)) return RedirectToAction("Index", "Booking");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var booking = db.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomCategory)
                .FirstOrDefault(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null || booking.Status != BookingStatus.PENDING)
            {
                TempData["Error"] = "Invalid booking or already paid.";
                return RedirectToAction("BookingHistory", "Booking");
            }

            var vm = new PaymentVM
            {
                BookingId = booking.Id,
                Booking = booking
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Process(PaymentVM vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                vm.Booking = db.Bookings
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomCategory)
                    .FirstOrDefault(b => b.Id == vm.BookingId && b.UserId == userId);

                if (vm.Booking == null) return RedirectToAction("BookingHistory", "Booking");

                return View("Index", vm);
            }

            var strategy = db.Database.CreateExecutionStrategy();

            return strategy.Execute(() =>
            {
                using var transaction = db.Database.BeginTransaction();
                try
                {
                    var booking = db.Bookings.FirstOrDefault(b => b.Id == vm.BookingId && b.UserId == userId);

                    if (booking == null || booking.Status != BookingStatus.PENDING)
                    {
                        TempData["Error"] = "Booking unavailable or already paid.";
                        return RedirectToAction("BookingHistory", "Booking");
                    }

                    var newPayment = new Payment
                    {
                        Id = GeneratePaymentId(),
                        Type = PaymentType.BOOKING,
                        Method = PaymentMethod.CREDIT_CARD,
                        Status = PaymentStatus.SUCCESS,
                        Date = DateTime.Now,
                        BookingId = booking.Id,
                        FineId = null
                    };

                    db.Payments.Add(newPayment);

                    booking.Status = BookingStatus.CONFIRMED;

                    var room = db.Rooms.FirstOrDefault(r => r.Id == booking.RoomId);
                    if (room != null)
                    {
                        room.Status = RoomStatus.UNAVAILABLE;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Payment successful! Booking confirmed.";
                    return RedirectToAction("BookingHistory", "Booking");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "System Error: " + ex.Message;
                    return RedirectToAction("Index", new { bookingId = vm.BookingId });
                }
            });
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment cancelled. You can pay later from Booking History.";
            return RedirectToAction("BookingHistory", "Booking");
        }

        private string GeneratePaymentId()
        {
            var lastId = db.Payments.OrderByDescending(p => p.Id).Select(p => p.Id).FirstOrDefault();
            if (string.IsNullOrEmpty(lastId)) return "P00001";
            if (lastId.Length >= 6 && int.TryParse(lastId.Substring(1), out int num)) return "P" + (num + 1).ToString("D5");
            return "P" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        }
    }
}