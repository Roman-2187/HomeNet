using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNetPresentation.Models
{
    
    public partial class LogToken(string text, LogLevel level) : ObservableObject

    {
        [ObservableProperty]
        public partial string Text { get; set; } = text;
        public LogLevel Level { get; } = level;

        // Метод быстрого добавления символа без пересоздания строки
        public void Append(char c)
        {
            Text += c;
        }
    }
   
}





