using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public interface IFineService
{
    Task GenerateFines();
    List<Fine> GetUserFines(string userId);
}

public class FineService(LMSDB db, INotificationService ns) : IFineService
{
    public async Task GenerateFines()
    {
        var today = DateTime.Now.Date;
        var overdueLoans = await db.Loans
            .Include(l => l.Copy).ThenInclude(c => c.Book)
            .Where(l => (l.Status == Loan.LoanStatus.IN_LOAN || l.Status == Loan.LoanStatus.OVERDUE) && l.LoanDue < DateOnly.FromDateTime(today))
            .ToListAsync();

        var lastId = await db.Fines.OrderByDescending(f => f.Id).Select(f => f.Id).FirstOrDefaultAsync();

        foreach (var loan in overdueLoans)
        {
            if (loan.Status != Loan.LoanStatus.OVERDUE)
            {
                loan.Status = Loan.LoanStatus.OVERDUE;
                ns.Notify(loan.UserId, $"Alert: Your loan for '{loan.Copy.Book.Title}' is now overdue. Please return it immediately to avoid further fines.", "/Loan/History");
            }

            var daysOverdue = (today - loan.LoanDue.ToDateTime(TimeOnly.MinValue)).Days;
            if (daysOverdue <= 0) continue;

            decimal rate = 0.50m; // RM 0.50 per day
            decimal totalFineShouldBe = daysOverdue * rate;

            var existingUnpaidFine = await db.Fines.FirstOrDefaultAsync(f => f.LoanId == loan.Id && !f.IsPaid);

            if (existingUnpaidFine != null)
            {
                // Calculate balance by subtracting any previously paid fines for this loan
                var paidAmount = await db.Fines.Where(f => f.LoanId == loan.Id && f.IsPaid).SumAsync(f => f.Amount);
                decimal balance = totalFineShouldBe - paidAmount;

                if (balance > 0)
                {
                    existingUnpaidFine.Amount = balance;
                    // existingUnpaidFine.DueDate = DateTime.Now.AddDays(7); // Optional: Extend due date
                }
            }
            else
            {
                // No unpaid fine exists. Check if we need to create one.
                var paidAmount = await db.Fines.Where(f => f.LoanId == loan.Id && f.IsPaid).SumAsync(f => f.Amount);
                decimal balance = totalFineShouldBe - paidAmount;

                if (balance > 0)
                {
                    string newId = Helper.GenerateNewId(lastId, 'F');
                    lastId = newId;

                    var fine = new Fine
                    {
                        Id = newId,
                        Amount = balance,
                        IssuedDate = DateTime.Now,
                        NotificationDate = DateTime.Now,
                        DueDate = DateTime.Now.AddDays(7),
                        OverdueRate = rate,
                        IsPaid = false,
                        LoanId = loan.Id
                    };
                    db.Fines.Add(fine);
                    ns.Notify(loan.UserId, $"You have incurred a new fine of RM {fine.Amount:F2} for the overdue book '{loan.Copy.Book.Title}'.", "/Fine/Records");
                }
            }
        }

        await db.SaveChangesAsync();
    }

    public List<Fine> GetUserFines(string userId)
    {
        return db.Fines
            .Include(f => f.Loan)
            .ThenInclude(l => l.Copy)
            .ThenInclude(c => c.Book)
            .Where(f => f.Loan.UserId == userId)
            .OrderByDescending(f => f.IssuedDate)
            .ToList();
    }
}
