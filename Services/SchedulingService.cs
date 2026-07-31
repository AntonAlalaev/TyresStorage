using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TyresStorage.Models;
using TyresStorage.Services;

//При старте сразу же обрабатывает пропущенные запуски (если NextRun в прошлом).
//При успешном запуске обновляет LastRun и NextRun.
//При ошибке оставляет даты как есть, чтобы повторить на следующей проверке.
//Использует ILogger для записи событий.


namespace TyresStorage.Services
{
    public class SchedulingService : BackgroundService
    {
        private readonly IDeviceRepository _repository;
        private readonly IDeviceHttpClient _httpClient;
        private readonly ILogger<SchedulingService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public SchedulingService(
            IDeviceRepository repository,
            IDeviceHttpClient httpClient,
            ILogger<SchedulingService> logger)
        {
            _repository = repository;
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduling Service started.");

            // При старте выполняем проверку (включая обработку пропущенных запусков)
            await CheckAndRunAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndRunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduling check");
                }

                // Ждём интервал или токен отмены
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Scheduling Service stopped.");
        }

        private async Task CheckAndRunAsync(CancellationToken cancellationToken)
        {
            var devices = await _repository.GetAllAsync();
            var now = DateTime.Now;
            bool anyChanges = false;

            foreach (var device in devices.Where(d => d.IsEnabled))
            {
                // Если NextRun не задан — инициализируем от текущего времени + период
                if (device.NextRun == null)
                {
                    _logger.LogInformation($"Device {device.Id}: NextRun is null, initializing to now + {device.PeriodDays} days.");
                    device.NextRun = now + TimeSpan.FromDays(device.PeriodDays);
                    await _repository.UpdateAsync(device);
                    anyChanges = true;
                    continue;
                }

                // Если время пришло (или уже прошло) — запускаем
                if (now >= device.NextRun)
                {
                    _logger.LogInformation($"Device {device.Id}: scheduled run at {device.NextRun}, executing now.");

                    bool success = await _httpClient.SendStartCommandAsync(device, cancellationToken);
                    if (success)
                    {
                        device.LastRun = now;
                        device.NextRun = now + TimeSpan.FromDays(device.PeriodDays);
                        await _repository.UpdateAsync(device);
                        anyChanges = true;
                        _logger.LogInformation($"Device {device.Id}: started successfully, next run at {device.NextRun}.");
                    }
                    else
                    {
                        // Не удалось запустить — не меняем даты, попробуем снова при следующей проверке
                        _logger.LogWarning($"Device {device.Id}: start command failed, will retry later.");
                    }
                }
            }

            if (anyChanges)
            {
                await _repository.SaveChangesAsync();
            }
        }
    }
}