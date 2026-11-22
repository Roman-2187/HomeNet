using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums;
using HomeNetCore.Validation;

namespace HomeNetPresentation.ViewModels.MainViews
{
    /// <summary>
    /// Стерильный generic-фундамент для всех интерактивных форм проекта SiberNet.
    /// Строго типизирует навигатор для каждого конкретного экрана.
    /// </summary>
    public abstract partial class FormViewModelBase(IEventBus eventBus,ICastomLogger logger)
        : ObservableObject, IDisposable
    {
      

        protected  IEventBus EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        protected  ICastomLogger Logger= logger ?? throw new ArgumentNullException(nameof(logger));

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;
        [ObservableProperty]
        public partial string SubmitButtonText { get; set; } = "Выполнить";

        [ObservableProperty]
        private IReadOnlyDictionary<TypeField, ValidationResult> _validationResults
            = new Dictionary<TypeField, ValidationResult>();


        protected void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results.ToDictionary(r => r.Field, r => r);
        }

        /// <summary>
        /// Глобальный виртуальный стерилизатор для безопасной зачистки подписок в наследниках
        /// </summary>
        public virtual void Dispose()
        {
            // Базовая зачистка, если потребуется
        }
    }
}
