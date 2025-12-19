using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public class BookingReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingReminderService> _logger;

        public BookingReminderService(IServiceProvider serviceProvider, ILogger<BookingReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Reminder Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<LMSDB>();
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                        var now = DateTime.Now;
                        var targetTime = now.AddMinutes(15);

                        var upcomingBookings = await db.Bookings
                            .Include(b => b.User)
                            .Include(b => b.Room)
                            .Where(b => b.Status == Booking.BookingStatus.CONFIRMED
                                     && !b.IsReminderSent
                                     && b.StartTime <= targetTime
                                     && b.StartTime > now)
                            .ToListAsync(stoppingToken);

                        if (upcomingBookings.Any())
                        {
                            foreach (var booking in upcomingBookings)
                            {
                                string subject = "🔔 Reminder: Your Library Room Booking Starts Soon!";
                                string body = $@"
                                    <h3>Hi {booking.User.Username},</h3>
                                    <p>Just a friendly reminder that your booking for <strong>{booking.Room.Name}</strong> is starting in about 15 minutes.</p>
                                    <p><strong>Time:</strong> {booking.StartTime:yyyy-MM-dd hh:mm tt} - {booking.EndTime:hh:mm tt}</p>
                                    <p>Please arrive on time.</p>
                                    <br/>
                                    <p>Library Management Team</p>";

                                try
                                {
                                    await emailService.SendEmailAsync(booking.User.Email, subject, body);

                                    booking.IsReminderSent = true;
                                    _logger.LogInformation($"Reminder sent to {booking.User.Email} for booking {booking.Id}");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Failed to send email to booking {booking.Id}: {ex.Message}");
                                }
                            }

                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in BookingReminderService: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}