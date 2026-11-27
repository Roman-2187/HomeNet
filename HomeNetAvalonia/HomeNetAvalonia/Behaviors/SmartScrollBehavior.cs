using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace HomeNetAvalonia.Behaviors
{
    public static class SmartScrollBehavior
    {
        // Включает умный автоскролл на ListBox
        public static readonly AttachedProperty<bool> EnableProperty =
            AvaloniaProperty.RegisterAttached<ListBox, bool>("Enable", typeof(SmartScrollBehavior));

        // Опционально: привязка к плавающей кнопке "Вниз"
        public static readonly AttachedProperty<Button?> ScrollDownButtonProperty =
            AvaloniaProperty.RegisterAttached<ListBox, Button?>("ScrollDownButton", typeof(SmartScrollBehavior));

        static SmartScrollBehavior()
        {
            EnableProperty.Changed.AddClassHandler<ListBox>((listBox, e) =>
            {
                if (e.NewValue is true)
                    listBox.Loaded += OnListBoxLoaded;
                else
                    listBox.Loaded -= OnListBoxLoaded;
            });
        }

        public static void SetEnable(ListBox element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(ListBox element) => element.GetValue(EnableProperty);

        public static void SetScrollDownButton(ListBox element, Button? value) => element.SetValue(ScrollDownButtonProperty, value);
        public static Button? GetScrollDownButton(ListBox element) => element.GetValue(ScrollDownButtonProperty);

        private static void OnListBoxLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            var scrollViewer = listBox.FindDescendantOfType<ScrollViewer>();
            if (scrollViewer == null) return;

            var button = GetScrollDownButton(listBox);
            if (button != null)
            {
                button.Click += (_, _) => scrollViewer.ScrollToEnd();
            }

            // Отслеживаем прокрутку для показа/скрытия кнопки
            scrollViewer.ScrollChanged += (_, _) =>
            {
                if (button == null) return;
                var distanceFromBottom = scrollViewer.Extent.Height - scrollViewer.Viewport.Height - scrollViewer.Offset.Y;
                button.IsVisible = distanceFromBottom > 100;
            };

            // Подписываемся на добавление новых сообщений в коллекцию
            void AttachCollection(object? items)
            {
                if (items is INotifyCollectionChanged notifyCollection)
                {
                    notifyCollection.CollectionChanged += (_, args) =>
                    {
                        // Проверяем, был ли юзер у нижнего края ДО добавления сообщения
                        var distanceFromBottom = scrollViewer.Extent.Height - scrollViewer.Viewport.Height - scrollViewer.Offset.Y;
                        var isAtBottom = distanceFromBottom <= 120 || args.Action == NotifyCollectionChangedAction.Reset;

                        if (isAtBottom)
                        {
                            Dispatcher.UIThread.Post(scrollViewer.ScrollToEnd, DispatcherPriority.Background);
                        }
                    };
                }
            }

            AttachCollection(listBox.ItemsSource);
        
            // На случай если коллекция Messages пересоздаётся (Messages = [.. history])
            listBox.PropertyChanged += (_, args) =>
            {
                if (args.Property != ItemsControl.ItemsSourceProperty)
                {
                    return;
                }

                AttachCollection(args.NewValue);
                Dispatcher.UIThread.Post(scrollViewer.ScrollToEnd, DispatcherPriority.Background);
            };

            // Скроллим вниз при первом открытии
            Dispatcher.UIThread.Post(scrollViewer.ScrollToEnd, DispatcherPriority.Background);
        }
    }
}
