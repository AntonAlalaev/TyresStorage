using Microsoft.Extensions.Logging;
using TyresStorage.Models;
using TyresStorage.Services;

namespace TyresStorage.Services
{
    /// <summary>
    /// Заглушка для тестирования без реальных устройств
    /// </summary>
    public class FakeDeviceHttpClient : IDeviceHttpClient
    {
        private readonly ILogger<FakeDeviceHttpClient> _logger;

        public FakeDeviceHttpClient(ILogger<FakeDeviceHttpClient> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendStartCommandAsync(Device device, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"FAKE START: Device {device.Id} at {device.IpAddress} for {device.DurationSeconds} sec");
            // Всегда возвращаем успех
            return Task.FromResult(true);
        }
    }
}