using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using System.Collections.ObjectModel;
using static HomeNetCore.Interfaces.ViewModels.AdminZone.IUsersTableVm;
using static HomeNetCore.Interfaces.ViewModels.MainZone.IStatusBarViewModel;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IContactsVm;
using static HomeNetCore.Interfaces.ViewModels.UserZone.IUserVm;
using static HomeNetOrm.Interfaces.Repositories.IUserRepo;

namespace HomeNetPresentation.ViewModels.ClientViews
{
    /// <summary>
    /// Реактивный список контактов HomeNet.
    /// Полностью отвязан от СУБД и логики навигации чата! 🧼
    /// </summary>
    public partial class ContactsViewModel : ObservableObject, IContactsVm, IDisposable, IMenuViewModel
    {
        private readonly IEventBus _eventBus;

        [ObservableProperty]
        public partial bool IsMenuOpen { get; set; }

        private UserEntity? _currentUser;

        [ObservableProperty]
        public partial UserEntity? SelectedFriend { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<UserEntity> Friends { get; set; } = [];

        [ObservableProperty] private string _currentUserName = "Авторизация...";

        private Guid? _currentUserId;

        public ContactsViewModel(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            InitializeBusSubscriptions();
        }

        private void InitializeBusSubscriptions()
        {
            // Слушаем изменения таблицы пользователей в БД через центральный репозиторий
            _eventBus.Subscribe<Refreshed>(OnUsersRefreshed);

            // 🔥 ПЕРЕВОДИМ НА СИСТЕМНЫЕ СОБЫТИЯ СЛОЯ ДАННЫХ
            _eventBus.Subscribe<Inserted>(OnUserAdded);
            _eventBus.Subscribe<Deleted>(OnUserDeleted);

            _eventBus.Subscribe<UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Subscribe<UserSignedUp>(OnGlobalUserSignedUp);
        }

        private void OnGlobalUserSignedIn(UserSignedIn msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id;
            _currentUser = msg.User;
            CurrentUserName = msg.User.FirstName ?? "Вы";

            _eventBus.Publish(this, new RefreshRequest());
        }

        private void OnGlobalUserSignedUp(UserSignedUp msg)
        {
            if (msg?.User == null) return;

            _currentUserId = msg.User.Id;
            _currentUser = msg.User;
            CurrentUserName = msg.User.FirstName ?? "Вы";

            _eventBus.Publish(this, new RefreshRequest());
        }

        private void OnUsersRefreshed(Refreshed msg)
        {
            if (msg?.Users == null) return;

            
            var filtered = msg.Users
                .Where(u => _currentUserId == null || u.Id != _currentUserId.Value)
                .Select(u =>
                {
                    u.FirstName ??= "Пользователь без имени";
                    return u;
                });

            Friends = [.. filtered];
        }

        [RelayCommand]
        private void Logout()
        {
            IsMenuOpen = false;
            CurrentUserName = "Авторизация...";
            _eventBus.Publish(this, new LogoutClient());
        }

        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }


        // 🔥 Только закрывает (для кликов мимо)
        [RelayCommand]
        private void CloseMenu() => IsMenuOpen = false;



        private void OnUserAdded(Inserted msg)
        {
            if (msg?.User == null) return;

            // 1. Не добавляем самого себя
            if (_currentUser != null && msg.User.Id == _currentUser.Id) return;

            // 2. 🔥 ЗАЩИТА ОТ ДВОЙНИКОВ: если такой юзер уже есть в списке, не добавляем его снова!
            if (Friends.Any(f => f.Id == msg.User.Id)) return;

            msg.User.FirstName ??= "Пользователь без имени";

            Friends.Add(msg.User);
        }


        // 🔥 Метод теперь принимает системный тип события от репозитория
        private void OnUserDeleted(Deleted? msg)
        {
            if (msg == null) return;
            var friendToRemove = Friends.FirstOrDefault(f => f.Id == msg.UserId);
            if (friendToRemove != null) Friends.Remove(friendToRemove);
        }

        partial void OnSelectedFriendChanged(UserEntity? value)
        {
            if (value == null)
            {
                return;
            }

            _eventBus.Publish(this, new FriendSelected(value, _currentUserId ?? Guid.Empty));

            _eventBus.Publish(this, new TextChanged($"собеседник: {SelectedFriend?.FirstName??"unknown"}"));
        }

        public void SetCurrentUser(UserEntity? currentUser)
        {
            _currentUser = currentUser;
            if (_currentUser != null)
            {
                CurrentUserName = _currentUser.FirstName ?? "Вы";
            }
            _eventBus.Publish(this, new RefreshRequest());
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<Refreshed>(OnUsersRefreshed);

            // 🔥 Не забываем отписаться от новых интерфейсов в Dispose
            _eventBus.Unsubscribe<Inserted>(OnUserAdded);
            _eventBus.Unsubscribe<Deleted>(OnUserDeleted);

            _eventBus.Unsubscribe<UserSignedIn>(OnGlobalUserSignedIn);
            _eventBus.Unsubscribe<UserSignedUp>(OnGlobalUserSignedUp);
        }
    }
}
