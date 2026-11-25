using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using System.Collections.ObjectModel;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IContactsVm;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;
using static HomeNetOrm.Interfaces.Repositories.IMessageRepo;

namespace HomeNetPresentation.ViewModels.ClientViews.Chat
{
    public sealed partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageService _messageService;

        [ObservableProperty]
        public partial ChatStateModel State { get; set; } = new();

        [ObservableProperty]
        public partial ObservableCollection<MessageItemViewModel> Messages { get; set; } = [];

        public ChatViewModel(IEventBus eventBus, IMessageService messageService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            

            _eventBus.Subscribe<UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<UserSignedUp>(OnUserSignedUp);
            _eventBus.SubscribeAsync<FriendSelected>(OnFriendSelected);
            _eventBus.Subscribe<Saved>(OnMessageSaved);
            _eventBus.Subscribe<LogoutClient>(OnLogout);
        }

       

        private void OnLogout(LogoutClient msg)
        {
            Messages.Clear();
            State.ResetSession();
        }

        private void OnUserSignedIn(UserSignedIn msg) => ApplyUserSession(msg?.User?.Id, msg?.User?.FirstName);

        private void OnUserSignedUp(UserSignedUp msg) => ApplyUserSession(msg?.User?.Id, msg?.User?.FirstName);

        private void ApplyUserSession(Guid? userId, string? firstName)
        {
            if (userId == null || userId == Guid.Empty) return;

            if (State.CurrentUserId != userId.Value)
            {
                Messages.Clear();
                State.ResetActiveChat();
            }

            State.CurrentUserId = userId.Value;
            State.CurrentUserName = firstName ?? "Вы";
        }

        private async Task OnFriendSelected(FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            State.CurrentUserId = msg.CurrentUserId;
            State.SelectedFriendId = msg.Friend.Id;
            State.SelectedFriendName = msg.Friend.FirstName ?? "Собеседник";
            State.IsChatOpen = true;
            Messages.Clear();

            try
            {
                var history = await _messageService.GetPrivateHistoryAsync(State.CurrentUserId, State.SelectedFriendId.Value);
                Messages = [.. history.Select(m => new MessageItemViewModel(m, State.CurrentUserId))];
            }
            catch (Exception ex)
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ИСТОРИИ]: {ex.Message}"));
            }
        }

       
        
        
        [RelayCommand]
        private async Task SendMessageAsync()
        {
            var text = State.InputText.Trim();
            if (text.Length == 0 || State.SelectedFriendId is not { } friendId)
                return;

            State.InputText = string.Empty;

            var verdict = await _messageService.SendPrivateMessageAsync(State.CurrentUserId, friendId, text);

            if (!verdict.IsValid)
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
        }



        [RelayCommand]
        private async Task DeleteMessageAsync(MessageItemViewModel? message)
        {
            if (message == null || message.IsDeleted || !message.IsOutgoing) return;

            var verdict = await _messageService.DeleteMessageAsync(message.Id);

            if (verdict.IsValid)
            {
                message.MarkAsDeleted();
            }
            else
            {
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА УДАЛЕНИЯ]: {verdict.ErrorMessage}"));
            }
        }

        private void OnMessageSaved(Saved msg)
        {
            if (msg?.Message == null || State.SelectedFriendId == null) return;

            var isCurrentChat =
                (msg.Message.SenderId == State.SelectedFriendId.Value && msg.Message.ReceiverId == State.CurrentUserId) ||
                (msg.Message.SenderId == State.CurrentUserId && msg.Message.ReceiverId == State.SelectedFriendId.Value);

            if (!isCurrentChat) return;

            if (Messages.All(m => m.Id != msg.Message.Id))
            {
                Messages.Add(new MessageItemViewModel(msg.Message, State.CurrentUserId));
            }
        }

        private bool CanSendMessage() =>
            !string.IsNullOrWhiteSpace(State.InputText) && State.SelectedFriendId != null;

        public void Dispose()
        {
            _eventBus.Unsubscribe<UserSignedIn>(OnUserSignedIn);
            _eventBus.Unsubscribe<UserSignedUp>(OnUserSignedUp);
            _eventBus.UnsubscribeAsync<FriendSelected>(OnFriendSelected);
            _eventBus.Unsubscribe<Saved>(OnMessageSaved);
            _eventBus.Unsubscribe<LogoutClient>(OnLogout);
        }
    }
}
