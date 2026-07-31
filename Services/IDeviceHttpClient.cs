using TyresStorage.Models;

namespace TyresStorage.Services
{
    public interface IDeviceHttpClient
    {
        /// <summary>
        /// Отправляет команду запуска на устройство
        /// </summary>
        /// <param name="device">Устройство</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>True, если запрос успешен (код 200-299), иначе False</returns>
        Task<bool> SendStartCommandAsync(Device device, CancellationToken cancellationToken = default);
    }
}