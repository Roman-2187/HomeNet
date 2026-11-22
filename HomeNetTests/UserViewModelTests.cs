using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNet.Models.Shared.Entity;
using HomeNetCore.Enums.Navigation;
using HomeNetCore.Interfaces.ViewModels.UserZone;
using HomeNetPresentation.Serviсes;
using HomeNetPresentation.ViewModels;
using HomeNetPresentation.ViewModels.ClientViews;
using HomeNetPresentation.ViewModels.MainViews;
using Moq;
using Assert = Xunit.Assert;

namespace HomeNetTests
{
    public class UserNavigationFlowTests
    {
        [Fact]
        public void When_OpenRegistrationRequested_Then_UserViewModel_ShouldReceive_RegistrationTab()
        {
            // 1. ARRANGE (Подготовка окружения)
            Mock<IEventBus> eventBusMock = new();
            Mock<ICastomLogger> loggerMock = new();

            // Списки для хранения колбэков, которые компоненты зарегистрируют в шине
            Action<IUserVm.OpenRegistration>? navigatorOpenRegCallback = null;
            Action<IUserVm.ClientTabChanged>? userVmTabChangedCallback = null;

            // Перехватываем подписку Навигатора на запрос регистрации
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.OpenRegistration>>()))
                .Callback<Action<IUserVm.OpenRegistration>>(action => navigatorOpenRegCallback = action);

            // Перехватываем подписку UserViewModel на изменение табов
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Перехватываем момент, когда Навигатор делает EventBus.Publish(...)
            // и СРАЗУ перенаправляем это событие в UserViewModel, имитируя работу реального автобуса!
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<IUserVm.ClientTabChanged>()))
                .Callback<object, IUserVm.ClientTabChanged>((sender, msg) =>
                {
                    // Если Навигатор опубликовал извещение, мы мгновенно передаем его в UserVM
                    userVmTabChangedCallback?.Invoke(msg);
                });

            // Инициализируем систему (они сами подписываются на мок шины)
            UserNavigationManager navigator = new(eventBusMock.Object, loggerMock.Object);
            UserViewModel userViewModel = new(loggerMock.Object, eventBusMock.Object);

            // Проверяем, что все подписки успешно перехвачены моком
            Assert.NotNull(navigatorOpenRegCallback);
            Assert.NotNull(userVmTabChangedCallback);

            // Создаем заглушку для сообщения "Хочу регистрацию" (например, от нажатия кнопки на панели)
            Mock<IUserVm.OpenRegistration> openRegMsgMock = new();

            // 2. ACT (Действие — имитируем клик/команду на регистрацию)
            // Вызываем колбэк навигатора, как будто в шину прилетела команда IUserVm.OpenRegistration
            navigatorOpenRegCallback.Invoke(openRegMsgMock.Object);

            // 3. ASSERT (Проверка результата)
            // Проверяем, долетел ли энум Registration до свойства вью-модели пользователя
            Assert.Equal(ClientSubTab.Registration, userViewModel.ActiveClientTab);
        }


        [Fact]
        public void When_OpenAuthRequested_Then_UserViewModel_ShouldReceive_AuthenticationTab()
        {
            // 1. ARRANGE (Подготовка окружения)
            Mock<IEventBus> eventBusMock = new();
            Mock<ICastomLogger> loggerMock = new();

            Action<IUserVm.OpenAuth>? navigatorOpenAuthCallback = null;
            Action<IUserVm.ClientTabChanged>? userVmTabChangedCallback = null;

            // Перехватываем подписку Навигатора на запрос Входа
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.OpenAuth>>()))
                .Callback<Action<IUserVm.OpenAuth>>(action => navigatorOpenAuthCallback = action);

            // Перехватываем подписку UserViewModel на изменение табов
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Перенаправляем событие изменения таба из Навигатора прямо во Вью-модель
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<IUserVm.ClientTabChanged>()))
                .Callback<object, IUserVm.ClientTabChanged>((sender, msg) =>
                {
                    userVmTabChangedCallback?.Invoke(msg);
                });

            // Инициализируем компоненты
            UserNavigationManager navigator = new(eventBusMock.Object, loggerMock.Object);
            UserViewModel userViewModel = new(loggerMock.Object, eventBusMock.Object);

            // Проверяем, что подписки успешно перехвачены
            Assert.NotNull(navigatorOpenAuthCallback);
            Assert.NotNull(userVmTabChangedCallback);

            // Создаем заглушку для сообщения "Хочу войти" (имитация клика по кнопке "Вход")
            Mock<IUserVm.OpenAuth> openAuthMsgMock = new();

            // 2. ACT (Действие)
            // Имитируем, что в шину прилетела команда IUserVm.OpenAuth
            navigatorOpenAuthCallback.Invoke(openAuthMsgMock.Object);

            // 3. ASSERT (Проверка)
            // Проверяем, что во вью-модели теперь стейт Authentication
            Assert.Equal(ClientSubTab.Authentication, userViewModel.ActiveClientTab);
        }

        [Fact]
        public void When_UserSignedIn_Then_UserViewModel_ShouldReceive_MessengerTab()
        {
            // 1. ARRANGE (Подготовка окружения)
            Mock<IEventBus> eventBusMock = new();
            Mock<ICastomLogger> loggerMock = new();

            // Коллекция для хранения ВСЕХ подписчиков на событие успешного входа
            List<Action<IUserVm.UserSignedIn>> signedInCallbacks = [];
            Action<IUserVm.ClientTabChanged>? userVmTabChangedCallback = null;

            // Универсальный перехватчик подписок на UserSignedIn. 
            // Каждый раз, когда кто-то (Навигатор или ЮзерВМ) вызывает Subscribe, мы добавляем его колбэк в список.
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.UserSignedIn>>()))
                .Callback<Action<IUserVm.UserSignedIn>>(signedInCallbacks.Add);

            // Перехватываем подписку UserViewModel на изменение вкладок
            eventBusMock
                .Setup(bus => bus.Subscribe(It.IsAny<Action<IUserVm.ClientTabChanged>>()))
                .Callback<Action<IUserVm.ClientTabChanged>>(action => userVmTabChangedCallback = action);

            // Имитируем работу реальной шины: когда Навигатор публикует ClientTabChanged, 
            // мок сразу же перенаправляет это событие во Вью-Модель
            eventBusMock
                .Setup(bus => bus.Publish(It.IsAny<object>(), It.IsAny<object>()))
                .Callback<object, object>((sender, msg) =>
                {
                    if (msg is IUserVm.ClientTabChanged tabChangedMsg)
                    {
                        userVmTabChangedCallback?.Invoke(tabChangedMsg);
                    }
                });

            // Инициализируем компоненты (они внутри себя вызовут Subscribe, и наши списки заполнятся)
            UserNavigationManager navigator = new(eventBusMock.Object, loggerMock.Object);
            UserViewModel userViewModel = new(loggerMock.Object, eventBusMock.Object);

            // Создаем реальные объекты данных для отправки
            UserEntity realUser = new();
            IUserVm.UserSignedIn realMessage = new(realUser);

            // 2. ACT (Действие)
            // Имитируем падение события UserSignedIn в шину — оповещаем ВСЕХ зарегистрированных подписчиков
            foreach (var callback in signedInCallbacks)
            {
                callback.Invoke(realMessage);
            }

            // 3. ASSERT (Проверка результата)
            // Теперь Навигатор гарантированно обработал ивент, переключил стейт на Messenger и пнул Вью-Модель!
            Assert.Equal(ClientSubTab.Messenger, userViewModel.ActiveClientTab);
        }








    }
}
