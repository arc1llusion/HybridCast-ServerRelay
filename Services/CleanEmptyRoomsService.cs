
using HybridCast_ServerRelay.Storage;

namespace HybridCast_ServerRelay.Services
{
    public class CleanEmptyRoomsService : BackgroundService
    {
        private readonly ILogger<CleanEmptyRoomsService> logger;
        public IServiceProvider ServiceProvider { get; private set; }

        private readonly IRoomStorage roomStorage;

        public CleanEmptyRoomsService(IServiceProvider services, ILogger<CleanEmptyRoomsService> logger)
        {
            this.logger = logger ?? throw new InvalidOperationException(nameof(ILogger<CleanEmptyRoomsService>));
            ServiceProvider = services ?? throw new InvalidOperationException(nameof(IServiceProvider));

            roomStorage = ServiceProvider.GetRequiredService<IRoomStorage>() ?? throw new InvalidOperationException(nameof(IRoomStorage));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Clean task starting");

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await Clean();
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Clean Service is stopping.");
            }
        }

        private async Task Clean()
        {
            logger.LogInformation($"Clean starting at: {DateTime.UtcNow.ToString()}");

            await roomStorage.CleanRooms();

            logger.LogInformation($"Clean ending at: {DateTime.UtcNow.ToString()}");
        }
    }
}
