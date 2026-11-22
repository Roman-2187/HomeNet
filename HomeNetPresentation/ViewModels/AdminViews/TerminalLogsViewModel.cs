using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Enums;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Diagnostics;
using System.Collections.ObjectModel;
using LogToken = HomeNetPresentation.Models.LogToken;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    public  class TerminalLogsViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;

        // Ссылка на текущую строку лога, которую мы сейчас анимируем
        private List<LogToken>? _currentLine;

      

        // Символы, после которых мы железно "отрезаем" слово и начинаем новое
        private readonly HashSet<char> _splitters = [' ', ']', ':', ',', ';'];

        // Главная коллекция строк лога для XAML
        public ObservableCollection<List<LogToken>> Logs { get; } = [];

        public TerminalLogsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
        }

        private void OnLogReceived(ILogQueueManager.LogMessageReceived msg)
        {
            if (msg.IsAnimating) // Идет кумулятивная печать строки
            {
                if (string.IsNullOrEmpty(msg.Text)) return;

                // 1. Парсим всю прилетевшую подросшую строку на отдельные слова
                // Используем те же разделители, но сохраняем их в тексте, чтобы логи не слиплись
                List<LogToken> tokensList = [];
                var currentWord = "";

                foreach (var c in msg.Text)
                {
                    currentWord += c;

                    // Если дошли до разделителя — фиксируем слово как отдельный токен
                    if (!_splitters.Contains(c))
                    {
                        continue;
                    }

                    tokensList.Add(new LogToken(currentWord, msg.Level));
                    currentWord = "";
                }

                // Если в конце осталось недописанное слово — тоже добавляем его в токены
                if (!string.IsNullOrEmpty(currentWord))
                {
                    tokensList.Add(new LogToken(currentWord, msg.Level));
                }

                // 2. Выводим результат в коллекцию
                if (_currentLine == null)
                {
                    // Самый первый символ строки
                    _currentLine = tokensList;
                    Logs.Add(_currentLine);
                    if (Logs.Count > 1000) Logs.RemoveAt(0);
                }
                else
                {
                    // 🔥 ТВОЯ РОДНАЯ ЖЕЛЕЗОБЕТОННАЯ ПОДМЕНА!
                    // Находим индекс и просто перезаписываем строчку новым списком слов
                    var index = Logs.IndexOf(_currentLine);
                    if (index < 0)
                    {
                        return;
                    }

                    _currentLine = tokensList;
                    Logs[index] = tokensList; // Шлём в XAML обновленный массив слов
                }
            }
            else // Конец сообщения (msg.IsAnimating == false)
            {
                if (_currentLine is { Count: > 0 })
                {
                    List<LogToken> emptyLine = [new LogToken(" ", LogLevel.Info)];
                    Logs.Add(emptyLine);
                }
                _currentLine = null;
               
            }
        }



        public void Dispose() => _eventBus.Unsubscribe<ILogQueueManager.LogMessageReceived>(OnLogReceived);
    }
}
