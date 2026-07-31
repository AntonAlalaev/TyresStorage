using TyresStorage.Models;

namespace TyresStorage.Services
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAllAsync();
        Task<Device?> GetByIdAsync(int id);

        /// <summary>
        /// помечает устройство как изменённое
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        Task UpdateAsync(Device device);

        /// <summary>
        /// записывает все изменения в файл
        /// </summary>
        /// <returns></returns>
        Task SaveChangesAsync(); 
    }
}
