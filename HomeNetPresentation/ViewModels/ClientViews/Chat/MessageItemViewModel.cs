using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Models.Shared.Entity;
using System.Globalization;

namespace HomeNetPresentation.ViewModels.ClientViews.Chat
{
    /// <summary>
    /// Реактивная модель одного пузыря сообщения с поддержкой мягкого удаления.
    /// </summary>
    public sealed partial class MessageItemViewModel(MessageEntity entity, Guid currentUserId) : ObservableObject
    {
        public Guid Id { get; } = entity.Id;
        public bool IsOutgoing { get; } = entity.SenderId == currentUserId;
        public string FormattedTime { get; } = entity.LocalCreatedAt.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

        [ObservableProperty]
        public partial string Text { get; set; } = entity.IsDeleted ? "Сообщение удалено" : entity.Text;

        [ObservableProperty]
        public partial bool IsDeleted { get; set; } = entity.IsDeleted;

        public void MarkAsDeleted()
        {
            IsDeleted = true;
            Text = "Сообщение удалено";
        }
    }
}