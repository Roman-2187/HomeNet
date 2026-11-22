using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.AdminZone;
using HomeNetCore.Interfaces.ViewModels.MainZone;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetPresentation.TestData;
using System.Diagnostics.CodeAnalysis;

namespace HomeNetPresentation.ViewModels.AdminViews
{
    /// <summary>
    /// Вьюмодель модуля автоматического сидинга базы данных HomeNet.
    /// Наследуется от дженерик-базы с указанием конкретного менеджера навигации админки.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public partial class SeedUsersViewModel : MainViews.FormViewModelBase, IDisposable
    {
        private readonly IUserRepo _userRepository;

        [ObservableProperty]
        public partial string SeedStatusText { get; set; } = "🔋 Система генерации готова к сидингу 50 пользователей...";

        [ObservableProperty]
        public partial bool IsSeedInProgress { get; set; }

        public SeedUsersViewModel(IEventBus eventBus,ICastomLogger logger,IUserRepo userRepository )
            : base(eventBus, logger)
        {
           _userRepository = userRepository;
            EventBus.Subscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

       

        private void OnAdminTabChanged(IAdminVm.AdminTabChanged? msg)
        {
            if (msg == null) return;

            if (msg.ActiveTab == AdminSubTab.SeedUsers)
            {
                SeedStatusText = "🔋 Система генерации готова к сидингу  пользователей...";
            }
        }

        
        [RelayCommand]
        private async Task ExecuteDataSeedingAsync()
        {
            if (IsSeedInProgress) return;

            IsSeedInProgress = true;
            SeedStatusText = "⚡ Запуск сидинга... Инициализация транзакций...";
            var addedCount = 0;

            try
            {
                var testUsers = DbSeedData.GetGeneratedUsers();

                foreach (var user in testUsers)
                {
                    var emailExists = await _userRepository.EmailExistsAsync(user.Email);
                    if (emailExists)
                    {
                        continue;
                    }

                    await _userRepository.InsertAsync(user);

                        
                    addedCount++;
                }

                SeedStatusText = addedCount > 0
                    ? $"⚔️ Дисциплина наведена! База успешно заселена. Добавлено: {addedCount} юзеров."
                    : "🔒 Сидинг отклонён: Все тестовые пользователи уже находятся в SQLite.";

                EventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            catch (Exception ex)
            {
                SeedStatusText = $"❌ Критический сбой сидинга: {ex.Message}";
                EventBus.Publish(this, new IStatusBarViewModel.TextChanged(SeedStatusText));
            }
            finally
            {
                IsSeedInProgress = false;
            }
        }
        

        public override void Dispose()
        {
            base.Dispose();
            EventBus.Unsubscribe<IAdminVm.AdminTabChanged>(OnAdminTabChanged);
        }

       
    }
}
