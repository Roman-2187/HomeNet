using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using HomeNetAvalonia.Controls;
using System.Windows.Input;
using SideMenuControl = HomeNetAvalonia.Controls.MainControls.SideMenuControl;

namespace HomeNetAvalonia.Behaviors
{
    public static class ClickCommandBehavior
    {
        public static readonly AttachedProperty<ICommand?> CommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>("Command", typeof(ClickCommandBehavior));

        static ClickCommandBehavior()
        {
            CommandProperty.Changed.AddClassHandler<Control>((control, args) =>
            {
                if (args.NewValue is ICommand)
                {
                    // 🔥 Используем Tunnel, чтобы ловить клик ДО того, как его перехватит TextBox
                    control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                }
                else
                {
                    control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                }
            });
        }

        public static ICommand? GetCommand(Control element) => element.GetValue(CommandProperty);
        public static void SetCommand(Control element, ICommand? value) => element.SetValue(CommandProperty, value);

        private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Control control) return;

            if (e.Source is Control source && source.FindAncestorOfType<SideMenuControl>() != null)
                return;

            var command = GetCommand(control);
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
            }
        }

    }
}