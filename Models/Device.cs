namespace TyresStorage.Models
{
    /// <summary>
    /// Устройство (секция стеллажа)
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Идентификатор устройства (номер секции) 1-18
        /// </summary>
        public int Id { get; set; } = 0;

        /// <summary>
        /// IP адрес устройства 192.168.1.101-192.168.1.118
        /// </summary>
        public string IpAddress
        {
            get
            {
                if (Id > 0 && Id < 10)
                    return "192.168.1.10" + Id.ToString();
                else if (Id >= 10 && Id < 100)
                    return "192.168.1.1" + Id.ToString();
                else if (Id >= 100 && Id < 255)
                    return "192.168.1." + Id.ToString();
                else
                    return "error IP not Identified!!!";
            }
        }

        /// <summary>
        /// Время работы устройства в секундах
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Время в днях, через которое необходимо запустить устройство заново
        /// </summary>
        public int PeriodDays { get; set; }

        /// <summary>
        /// Дата и время последнего запуска устройства
        /// </summary>
        public DateTime? LastRun { get; set; }

        /// <summary>
        /// Планируемая дата запуска устройства
        /// </summary>
        public DateTime? NextRun { get; set; }

        /// <summary>
        /// Можно отключить устройство временно
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Вычислимое свойство возвращает Период до запуска
        /// </summary>
        public TimeSpan Period => TimeSpan.FromDays(PeriodDays);
    }
}
