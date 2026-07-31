using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TyresStorage.Models;
using TyresStorage.Services;

namespace TyresStorage.Pages
{

    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IDeviceRepository _repository;
        private readonly IDeviceHttpClient _httpClient;
        private readonly ILogger<IndexModel> _logger;

        public List<Device> Devices { get; set; } = new();

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        public IndexModel(IDeviceRepository repository, IDeviceHttpClient httpClient, ILogger<IndexModel> logger)
        {
            _repository = repository;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            var all = await _repository.GetAllAsync();
            Devices = all.ToList();
            _logger.LogInformation("Page loaded with {Count} devices.", Devices.Count);
        }



        private IActionResult JsonSuccess(Device device)
        {
            return new JsonResult(new
            {
                success = true,
                device = new
                {
                    device.Id,
                    device.DurationSeconds,
                    device.PeriodDays,
                    device.IsEnabled,
                    LastRun = device.LastRun?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    NextRun = device.NextRun?.ToString("yyyy-MM-ddTHH:mm:ss")
                }
            });
        }

        
        public async Task<IActionResult> OnPostUpdateDurationAsync(int id, int? duration)
        {
            _logger.LogInformation($"UpdateDuration called with id={id}, duration={duration}");
            if (!duration.HasValue || duration.Value < 1)
                duration = 1;

            var device = await _repository.GetByIdAsync(id);
            if (device != null)
            {
                device.DurationSeconds = duration.Value;
                if (device.LastRun.HasValue)
                    device.NextRun = device.LastRun + device.Period;
                else
                    device.NextRun = DateTime.Now + device.Period;
                await _repository.UpdateAsync(device);
                await _repository.SaveChangesAsync();
                _logger.LogInformation($"Updated device {id}: Duration={duration}, NextRun={device.NextRun}");
            }

            if (IsAjaxRequest())
                return JsonSuccess(device);
            else
                return RedirectToPage();
        }

        
        public async Task<IActionResult> OnPostUpdatePeriodAsync(int id, int? periodDays)
        {
            _logger.LogInformation($"UpdatePeriod called with id={id}, periodDays={periodDays}");
            if (!periodDays.HasValue || periodDays.Value < 1)
                periodDays = 1;

            var device = await _repository.GetByIdAsync(id);
            if (device != null)
            {
                device.PeriodDays = periodDays.Value;
                if (device.LastRun.HasValue)
                    device.NextRun = device.LastRun + device.Period;
                else
                    device.NextRun = DateTime.Now + device.Period;
                await _repository.UpdateAsync(device);
                await _repository.SaveChangesAsync();
                _logger.LogInformation($"Updated device {id}: PeriodDays={periodDays}, NextRun={device.NextRun}");
            }

            if (IsAjaxRequest())
                return JsonSuccess(device);
            else
                return RedirectToPage();
        }

        
        public async Task<IActionResult> OnPostStartNowAsync(int id)
        {
            _logger.LogInformation($"StartNow called for device {id}");
            var device = await _repository.GetByIdAsync(id);
            bool success = false;
            if (device != null)
            {
                success = await _httpClient.SendStartCommandAsync(device);
                if (success)
                {
                    device.LastRun = DateTime.Now;
                    device.NextRun = device.LastRun + device.Period;
                    await _repository.UpdateAsync(device);
                    await _repository.SaveChangesAsync();
                    _logger.LogInformation($"Device {id} started manually, NextRun={device.NextRun}");
                }
                else
                {
                    _logger.LogWarning($"Manual start failed for device {id}");
                }
            }

            if (IsAjaxRequest())
            {
                if (success)
                    return JsonSuccess(device);
                else
                    return new JsonResult(new { success = false, error = "Не удалось запустить устройство" });
            }
            else
            {
                if (!success && device != null)
                    TempData["Error"] = $"Не удалось запустить устройство {id}";
                return RedirectToPage();
            }
        }

        
        public async Task<IActionResult> OnPostToggleEnabledAsync(int id)
        {
            _logger.LogInformation($"ToggleEnabled called for device {id}");
            var device = await _repository.GetByIdAsync(id);
            if (device != null)
            {
                device.IsEnabled = !device.IsEnabled;
                await _repository.UpdateAsync(device);
                await _repository.SaveChangesAsync();
                _logger.LogInformation($"Device {id} enabled status toggled to {device.IsEnabled}");
            }

            if (IsAjaxRequest())
                return JsonSuccess(device);
            else
                return RedirectToPage();
        }
    }
}