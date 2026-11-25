using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.Services;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using System.Collections.ObjectModel;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IContactsVm;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;
using static HomeNetOrm.Interfaces.Repositories.IMessageRepo;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    public sealed partial class ChatViewModel : ObservableObject, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly IMessageService _messageService;

        [ObservableProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        public partial Guid CurrentUserId { get; set; }

        [ObservableProperty]
        public partial string CurrentUserName { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsChatOpen { get; set; } = false;

        [ObservableProperty]
        public partial string InputText { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string SelectedFriendName { get; set; } = string.Empty;

        [ObservableProperty]
        // ReSharper disable once MemberCanBePrivate.Global
        public partial Guid? SelectedFriendId { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<MessageEntity> Messages { get; set; } = [];

        public ChatViewModel(IEventBus eventBus, IMessageService messageService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            _eventBus.Subscribe<UserSignedIn>(OnUserSignedIn);
            _eventBus.Subscribe<UserSignedUp>(OnUserSignedUp);
            _eventBus.SubscribeAsync<FriendSelected>(OnFriendSelected);
            _eventBus.Subscribe<Saved>(OnMessageSaved);
            
            // 🔥 Подписываемся на логаут, чтобы вычистить чат
            _eventBus.Subscribe<LogoutClient>(OnLogout);
        }

        // 🔥 Очищаем всё при логауте
        private void OnLogout(LogoutClient msg)
        {
            Messages.Clear();
            SelectedFriendId = null;
            SelectedFriendName = string.Empty;
            CurrentUserId = Guid.Empty;
            CurrentUserName = string.Empty;
            IsChatOpen = false;
            InputText = string.Empty;
        }

        private void OnUserSignedIn(UserSignedIn msg)
        {
            if (msg?.User == null) return;
            
            // 🔥 Если это новый пользователь (ID изменился), чистим старые сообщения
            if (CurrentUserId != msg.User.Id)
            {
                Messages.Clear();
                SelectedFriendId = null;
                IsChatOpen = false;
            }
            
            CurrentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName ?? "Вы";
        }

        private void OnUserSignedUp(UserSignedUp msg)
        {
            if (msg?.User == null) return;
            
            // 🔥 Та же логика при регистрации
            if (CurrentUserId != msg.User.Id)
            {
                Messages.Clear();
                SelectedFriendId = null;
                IsChatOpen = false;
            }

            CurrentUserId = msg.User.Id;
            CurrentUserName = msg.User.FirstName ?? "Вы";
        }

        private async Task  OnFriendSelected(FriendSelected msg)
        {
            if (msg?.Friend == null) return;

            CurrentUserId = msg.CurrentUserId;
            SelectedFriendId = msg.Friend.Id;
            SelectedFriendName = msg.Friend.FirstName ?? "Собеседник";

            Messages.Clear();
            IsChatOpen = true;

            try
            {
                var history = await _messageService.GetPrivateHistoryAsync(CurrentUserId, SelectedFriendId.Value);
                var messageEntities = history.ToList();
                foreach (var m in messageEntities)
                {
                    m.IsOutgoing = m.SenderId == CurrentUserId;
                }
                Messages = [.. messageEntities];
            }
            catch (Exception ex)
            {
                // ReSharper disable once MethodHasAsyncOverload
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ИСТОРИИ]: {ex.Message}"));
            }
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText) || SelectedFriendId == null) return;

            var rawText = InputText.Trim();
            InputText = string.Empty;

            var verdict = await _messageService.SendPrivateMessageAsync(CurrentUserId, SelectedFriendId.Value, rawText);

            if (!verdict.IsValid)
            {
                // ReSharper disable once MethodHasAsyncOverload
                _eventBus.Publish(this, new IStatusBarViewModel.TextChanged($"[ОШИБКА ЧАТА]: {verdict.ErrorMessage}"));
            }
        }

        private void OnMessageSaved(Saved msg)
        {
            if (msg?.Message == null || SelectedFriendId == null) return;

            if ((msg.Message.SenderId != SelectedFriendId.Value || msg.Message.ReceiverId != CurrentUserId) &&
                (msg.Message.SenderId != CurrentUserId || msg.Message.ReceiverId != SelectedFriendId.Value))
            {
                return;
            }

            msg.Message.IsOutgoing = msg.Message.SenderId == CurrentUserId;

            if (Messages.All(m => m.Id != msg.Message.Id))
            {
                Messages.Add(msg.Message);
            }
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(InputText) && SelectedFriendId != null;
        }

        partial void OnInputTextChanged(string value)
        {
            SendMessageCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedFriendIdChanged(Guid? value)
        {
            SendMessageCommand.NotifyCanExecuteChanged();
        }


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