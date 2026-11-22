using Avalonia;
using Avalonia.Controls;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.MainViews;
using System;

namespace HomeNetAvalonia.MainViews
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Публикуем событие через EventBus (без прямых зависимостей)
            if (Application.Current is App app)
            {
                app.EventBus.Publish(this, new WindowOpenedEvent
                {
                    ElapsedMs = App.AppStopwatch.ElapsedMilliseconds
                });
            }
        }


    }
}