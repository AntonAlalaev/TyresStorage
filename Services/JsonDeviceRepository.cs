using System.Text.Json;
using TyresStorage.Models;

namespace TyresStorage.Services
{
    public class JsonDeviceRepository : IDeviceRepository
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private List<Device> _devices = new();
        private bool _isLoaded = false;

        public JsonDeviceRepository(IWebHostEnvironment env)
        {
            // Файл будет лежать в папке Data внутри корня приложения
            var dataDir = Path.Combine(env.ContentRootPath, "Data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "devices.json");
        }

        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;

            await _semaphore.WaitAsync();
            try
            {
                if (_isLoaded) return;

                if (File.Exists(_filePath))
                {
                    var json = await File.ReadAllTextAsync(_filePath);
                    _devices = JsonSerializer.Deserialize<List<Device>>(json) ?? new List<Device>();
                }
                else
                {
                    // Создаём начальную конфигурацию для 18 устройств
                    _devices = Enumerable.Range(1, 18).Select(i => new Device
                    {
                        Id = i,
                        DurationSeconds = 10,        // значение по умолчанию
                        PeriodDays = 1,              // по умолчанию 1 день
                        LastRun = null,
                        NextRun = null,
                        IsEnabled = true
                    }).ToList();
                    await SaveChangesInternalAsync();
                }
                _isLoaded = true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<Device>> GetAllAsync()
        {
            await EnsureLoadedAsync();
            return _devices.ToList(); // возвращаем копию
        }

        public async Task<Device?> GetByIdAsync(int id)
        {
            await EnsureLoadedAsync();
            return _devices.FirstOrDefault(d => d.Id == id);
        }

        public async Task UpdateAsync(Device device)
        {
            await EnsureLoadedAsync();
            var existing = _devices.FirstOrDefault(d => d.Id == device.Id);
            if (existing != null)
            {
                // Обновляем поля
                existing.DurationSeconds = device.DurationSeconds;
                existing.PeriodDays = device.PeriodDays;
                existing.LastRun = device.LastRun;
                existing.NextRun = device.NextRun;
                existing.IsEnabled = device.IsEnabled;
            }
            else
            {
                _devices.Add(device);
            }
            // Сохраняем сразу? Нет, SaveChangesAsync вызовет запись.
        }

        public async Task SaveChangesAsync()
        {
            await EnsureLoadedAsync();
            await _semaphore.WaitAsync();
            try
            {
                await SaveChangesInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveChangesInternalAsync()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_devices, options);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
