namespace LibraryManagementSystem.Services;

public class FineBackgroundService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var fineService = scope.ServiceProvider.GetRequiredService<IFineService>();
                    await fineService.GenerateFines();
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
