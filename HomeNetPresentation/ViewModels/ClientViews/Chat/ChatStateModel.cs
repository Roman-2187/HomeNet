using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNetPresentation.ViewModels.ClientViews.Chat
{
    /// <summary>
    /// Реактивный контейнер состояния активного чата.
    /// </summary>
    public sealed partial class ChatStateModel : ObservableObject
    {
        [ObservableProperty] public partial Guid CurrentUserId { get; set; }
        [ObservableProperty] public partial string CurrentUserName { get; set; } = string.Empty;
        [ObservableProperty] public partial Guid? SelectedFriendId { get; set; }
        [ObservableProperty] public partial string SelectedFriendName { get; set; } = string.Empty;
        [ObservableProperty] public partial bool IsChatOpen { get; set; }
        [ObservableProperty] public partial string InputText { get; set; } = string.Empty;

        public void ResetSession()
        {
            CurrentUserId = Guid.Empty;
            CurrentUserName = string.Empty;
            ResetActiveChat();
        }

        public void ResetActiveChat()
        {
            SelectedFriendId = null;
            SelectedFriendName = string.Empty;
            IsChatOpen = false;
            InputText = string.Empty;
        }
    }
}