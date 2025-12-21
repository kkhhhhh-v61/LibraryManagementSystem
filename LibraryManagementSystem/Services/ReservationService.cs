using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IReservationService
{
    void AssignNextReservation(string bookId, string copyId);
}

public class ReservationService(LMSDB db, INotificationService ns) : IReservationService
{
    public void AssignNextReservation(string bookId, string copyId)
    {
        // 1. Find the oldest PENDING reservation for this book
        var reservation = db.Reservations
            .Where(r => r.BookId == bookId && r.Status == Reservation.ReservationStatus.PENDING)
            .OrderBy(r => r.ReservationDate)
            .FirstOrDefault();

        var copy = db.Copies.Include(c => c.Book).FirstOrDefault(c => c.Id == copyId);
        if (copy == null) return;

        if (reservation != null)
        {
            // 2. Assign to user
            reservation.Status = Reservation.ReservationStatus.READY;
            reservation.CopyId = copyId;
            reservation.ReservationDue = DateTime.Now.AddDays(3); // User has 3 days to confirm

            copy.Status = Copy.CopyStatus.RESERVED;

            ns.Notify(reservation.UserId, $"Good news! A copy of '{copy.Book.Title}' is now available for your reservation. Please confirm it within 3 days.", "/Reservation/Index");
        }
        else
        {
            // 3. No reservations, make copy available
            copy.Status = Copy.CopyStatus.AVAILABLE;
        }

        db.SaveChanges();
    }
}