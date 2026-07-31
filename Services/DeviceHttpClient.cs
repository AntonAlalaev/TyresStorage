using Microsoft.Extensions.Logging;
using System.Net.Http;
using TyresStorage.Models;

namespace TyresStorage.Services
{
    public class DeviceHttpClient : IDeviceHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DeviceHttpClient> _logger;

        public DeviceHttpClient(IHttpClientFactory httpClientFactory, ILogger<DeviceHttpClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendStartCommandAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var url = $"http://{device.IpAddress}/start?time={device.DurationSeconds}";
            var client = _httpClientFactory.CreateClient(); // используем фабрику для управления подключениями

            try
            {
                _logger.LogInformation($"Sending start command to {url}");
                var response = await client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Successfully started device {device.Id} at {device.IpAddress}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Failed to start device {device.Id}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error while starting device {device.Id} at {device.IpAddress}");
                return false;
            }
            catch (TaskCanceledException ex) // таймаут
            {
                _logger.LogError(ex, $"Timeout while starting device {device.Id} at {device.IpAddress}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error while starting device {device.Id} at {device.IpAddress}");
                return false;
            }
        }
    }
}