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
        public IActionResult Index(string? bookingId, string? fineId)
        {
            if (string.IsNullOrEmpty(bookingId) && string.IsNullOrEmpty(fineId)) 
                return RedirectToAction("Index", "Home");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vm = new PaymentVM();

            if (!string.IsNullOrEmpty(bookingId))
            {
                var booking = db.Bookings
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomCategory)
                    .FirstOrDefault(b => b.Id == bookingId && b.UserId == userId);

                if (booking == null || booking.Status != BookingStatus.PENDING)
                {
                    TempData["Error"] = "Invalid booking or already paid.";
                    return RedirectToAction("BookingHistory", "Booking");
                }
                
                vm.BookingId = booking.Id;
                vm.Booking = booking;
            }
            else if (!string.IsNullOrEmpty(fineId))
            {
                var fine = db.Fines
                    .Include(f => f.Loan)
                    .ThenInclude(l => l.Copy)
                    .ThenInclude(c => c.Book)
                    .FirstOrDefault(f => f.Id == fineId && f.Loan.UserId == userId);

                if (fine == null || fine.IsPaid)
                {
                    TempData["Error"] = "Invalid fine or already paid.";
                    return RedirectToAction("Records", "Fine");
                }

                vm.FineId = fine.Id;
                vm.Fine = fine;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Process(PaymentVM vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(vm.BookingId))
                {
                    vm.Booking = db.Bookings
                        .Include(b => b.Room)
                        .ThenInclude(r => r.RoomCategory)
                        .FirstOrDefault(b => b.Id == vm.BookingId && b.UserId == userId);

                    if (vm.Booking == null) return RedirectToAction("BookingHistory", "Booking");
                }
                else if (!string.IsNullOrEmpty(vm.FineId))
                {
                    vm.Fine = db.Fines
                        .Include(f => f.Loan)
                        .ThenInclude(l => l.Copy)
                        .ThenInclude(c => c.Book)
                        .FirstOrDefault(f => f.Id == vm.FineId && f.Loan.UserId == userId);

                    if (vm.Fine == null) return RedirectToAction("Records", "Fine");
                }
                else return RedirectToAction("Index", "Home");

                return View("Index", vm);
            }

            var strategy = db.Database.CreateExecutionStrategy();

            return strategy.Execute(() =>
            {
                using var transaction = db.Database.BeginTransaction();
                try
                {
                    if (!string.IsNullOrEmpty(vm.BookingId))
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

                        db.SaveChanges();
                        transaction.Commit();

                        TempData["Success"] = "Payment successful! Booking confirmed.";
                        return RedirectToAction("BookingHistory", "Booking");
                    }
                    else if (!string.IsNullOrEmpty(vm.FineId))
                    {
                        var fine = db.Fines.FirstOrDefault(f => f.Id == vm.FineId && f.Loan.UserId == userId);
                        if (fine == null || fine.IsPaid)
                        {
                            TempData["Error"] = "Fine unavailable or already paid.";
                            return RedirectToAction("Records", "Fine");
                        }

                        var newPayment = new Payment
                        {
                            Id = GeneratePaymentId(),
                            Type = PaymentType.FINE,
                            Method = PaymentMethod.CREDIT_CARD,
                            Status = PaymentStatus.SUCCESS,
                            Date = DateTime.Now,
                            BookingId = null,
                            FineId = fine.Id
                        };

                        db.Payments.Add(newPayment);
                        fine.IsPaid = true;
                        db.SaveChanges();
                        transaction.Commit();

                        TempData["Success"] = "Payment successful! Fine settled.";
                        return RedirectToAction("Records", "Fine");
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "System Error: " + ex.Message;
                    return RedirectToAction("Index", new { bookingId = vm.BookingId, fineId = vm.FineId });
                }
            });
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            TempData["Error"] = "Payment cancelled.";
            return RedirectToAction("Index", "Home");
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