using CommunityToolkit.Mvvm.ComponentModel;
using HomeNet.Infrastructure.Shared.Interfaces.Diagnostics;
using HomeNet.Infrastructure.Shared.Interfaces.Routing;
using HomeNetCore.Enums;
using HomeNetCore.Validation;

namespace HomeNetPresentation.ViewModels.MainViews
{
    /// <summary>
    /// Стерильный фундамент для всех интерактивных форм проекта HomeNet.
    /// </summary>
    public abstract partial class FormViewModelBase(IEventBus eventBus, ICastomLogger logger)
        : ObservableObject, IDisposable
    {
        protected IEventBus EventBus { get; } = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        protected ICastomLogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string SubmitButtonText { get; set; } = "Выполнить";

        [ObservableProperty]
        public partial IReadOnlyDictionary<TypeField, ValidationResult> ValidationResults { get; set; } 
            = new Dictionary<TypeField, ValidationResult>();

        /// <summary>
        /// Полностью обновляет словарь валидации из коллекции результатов.
        /// </summary>
        protected void UpdateValidation(IEnumerable<ValidationResult> results)
        {
            ValidationResults = results
                .GroupBy(r => r.Field)
                .ToDictionary(g => g.Key, g => g.First());
        }

        /// <summary>
        /// Точечно обновляет статус валидации для одного конкретного поля.
        /// </summary>
        protected void SetFieldValidation(ValidationResult result)
        {
            var updated = new Dictionary<TypeField, ValidationResult>(ValidationResults)
            {
                [result.Field] = result
            };
            ValidationResults = updated;
        }

        /// <summary>
        /// Очищает все текущие ошибки и подсказки валидации.
        /// </summary>
        protected void ClearValidation()
        {
            ValidationResults = new Dictionary<TypeField, ValidationResult>();
        }

        /// <summary>
        /// Глобальный виртуальный стерилизатор для безопасной зачистки подписок в наследниках.
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
