using web_DACS.Services.Interfaces;

namespace web_DACS.Services.Implementations
{
    public class BookingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public BookingWorker(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IDatBanService>();
                    await service.ProcessExpiredPendingBookingsAsync();
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}