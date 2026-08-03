using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TyresStorage.Pages
{
    public class DateTimeModel : PageModel
    {
        private readonly ILogger<DateTimeModel> _logger;

        [BindProperty]
        public int Day { get; set; } = DateTime.Now.Day;

        [BindProperty]
        public int Month { get; set; } = DateTime.Now.Month;

        [BindProperty]
        public int Year { get; set; } = DateTime.Now.Year;

        [BindProperty]
        public int Hour { get; set; } = DateTime.Now.Hour;

        [BindProperty]
        public int Minute { get; set; } = DateTime.Now.Minute;

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; } = false;

        public DateTimeModel(ILogger<DateTimeModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var now = DateTime.Now;
            Day = now.Day;
            Month = now.Month;
            Year = now.Year;
            Hour = now.Hour;
            Minute = now.Minute;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsValidDate(Year, Month, Day))
            {
                Message = "❌ Некорректная дата. Проверьте день, месяц и год.";
                IsSuccess = false;
                return Page();
            }
            if (Hour < 0 || Hour > 23 || Minute < 0 || Minute > 59)
            {
                Message = "❌ Некорректное время. Часы от 0 до 23, минуты от 0 до 59.";
                IsSuccess = false;
                return Page();
            }

            // Формируем строку для timedatectl (формат: YYYY-MM-DD HH:MM:SS)
            var dateTimeString = $"{Year:D4}-{Month:D2}-{Day:D2} {Hour:D2}:{Minute:D2}:00";

            // Используем timedatectl для установки времени (он учитывает часовой пояс)
            var command = $"sudo timedatectl set-time \"{dateTimeString}\" && sudo hwclock -w";

            try
            {
                _logger.LogInformation($"Setting system time to {dateTimeString}");
                var (output, error) = await ExecuteBashCommand(command);

                if (!string.IsNullOrEmpty(error) && !error.Contains("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    Message = $"❌ Ошибка: {error}";
                    IsSuccess = false;
                    _logger.LogError($"timedatectl error: {error}");
                }
                else
                {
                    Message = $"✅ Время успешно установлено: {dateTimeString}";
                    IsSuccess = true;
                    // Обновляем поля для отображения
                    var now = DateTime.Now;
                    Day = now.Day;
                    Month = now.Month;
                    Year = now.Year;
                    Hour = now.Hour;
                    Minute = now.Minute;
                    _logger.LogInformation($"Time set successfully. Current system time: {now}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set system time");
                Message = $"❌ Ошибка: {ex.Message}";
                IsSuccess = false;
            }

            return Page();
        }

        private bool IsValidDate(int year, int month, int day)
        {
            try
            {
                var _ = new DateTime(year, month, day);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<(string output, string error)> ExecuteBashCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (output, error);
        }

        // AJAX-метод для получения текущего времени системы (в локальном часовом поясе)
        public IActionResult OnGetCurrentTime()
        {
            return Content(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
        }
    }
}