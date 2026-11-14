using HomeNet.Diagnostics.Shared.Enums;

namespace HomeNet.Diagnostics.Shared
{
    // Легковесный, иммутабельный контейнер для трубы
    public readonly struct LogQueueEntry
    {
        public string Message { get; }
        public LogLevel Level { get; }

        public LogQueueEntry(string message, LogLevel level)
        {
            Message = message;
            Level = level;
        }
    }
}
