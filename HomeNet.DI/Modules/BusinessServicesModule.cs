using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Interfaces.Services;
using HomeNetOrm.Interfaces.Repositories;
using HomeNetOrm.Repositories.Local;
using HomeNetServices.Identity;
using HomeNet.Network.Api;

namespace HomeNet.DI.Modules
{
    public class BusinessServicesModule
    {
        // Оставляем свойство, если оно нужно для других модулей (например, для UI)
        public IUserRepo UserRepository { get; }

        public IRegistrationService RegistrationService { get; }
        public IAuthenticateService AuthenticateService { get; }
        public IDeleteService DeleteService { get; }
        public IMessageService MessageService { get; }
        public IDeviceSettingsService DeviceSettingsService { get; }

        public BusinessServicesModule(
            IUserRepo userRepo,          // Нужен для чтения (кэш)
            IMessageRepo msgRepo,        // Нужен для чтения (кэш)
            DeviceSettingsRepo deviceSettingsRepo, // Локальные настройки
            IUserApiClient userApiClient,   // 🔥 Новый клиент
            IMessageApiClient messageApiClient, // 🔥 Новый клиент
            ICastomLogger logger,
            IEventBus eventBus)
        {
            UserRepository = userRepo;
            DeviceSettingsService = new SettingsService(deviceSettingsRepo, userApiClient, logger);        
            RegistrationService = new RegistrationService(userApiClient);
            AuthenticateService = new AuthenticateService(userApiClient);
            DeleteService = new DeleteService(userApiClient, logger);
            MessageService = new MessageService(messageApiClient, msgRepo); // Передали API и репо для чтения

            // Настройки устройства остаются локальными
            // Настройки устройства остаются локальными + управляют заголовком сессии в ApiClient
           

        }
    }
}