using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services;

public class LoanBackgroundService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<LMSDB>();
                    var ns = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var today = DateOnly.FromDateTime(DateTime.Now);
                    var threeDaysLater = today.AddDays(3);
                    var oneDayLater = today.AddDays(1);

                    // 1. Check for loans due in 3 days
                    var loansDueIn3Days = await db.Loans
                        .Include(l => l.Copy).ThenInclude(c => c.Book)
                        .Where(l => l.Status == Loan.LoanStatus.IN_LOAN && l.LoanDue == threeDaysLater)
                        .ToListAsync(stoppingToken);

                    foreach (var loan in loansDueIn3Days)
                    {
                        // Prevent duplicate notifications if service restarts
                        bool exists = await db.Notifications.AnyAsync(n => 
                            n.UserId == loan.UserId && 
                            n.Message.Contains(loan.Copy.Book.Title) && 
                            n.Message.Contains("3 days") &&
                            n.CreatedAt.Date == DateTime.Today, stoppingToken);

                        if (!exists)
                        {
                            ns.Notify(loan.UserId, $"Reminder: Your loan for '{loan.Copy.Book.Title}' is due in 3 days ({loan.LoanDue:yyyy-MM-dd}).", "/Loan/History");
                        }
                    }

                    // 2. Check for loans due in 1 day
                    var loansDueIn1Day = await db.Loans
                        .Include(l => l.Copy).ThenInclude(c => c.Book)
                        .Where(l => l.Status == Loan.LoanStatus.IN_LOAN && l.LoanDue == oneDayLater)
                        .ToListAsync(stoppingToken);

                    foreach (var loan in loansDueIn1Day)
                    {
                        bool exists = await db.Notifications.AnyAsync(n => 
                            n.UserId == loan.UserId && 
                            n.Message.Contains(loan.Copy.Book.Title) && 
                            n.Message.Contains("tomorrow") &&
                            n.CreatedAt.Date == DateTime.Today, stoppingToken);

                        if (!exists)
                        {
                            ns.Notify(loan.UserId, $"Reminder: Your loan for '{loan.Copy.Book.Title}' is due tomorrow ({loan.LoanDue:yyyy-MM-dd}).", "/Loan/History");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // In production, log this error
            }

            // Run every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
