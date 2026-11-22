using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HomeNet.Models.Shared.Entity
{
    /// <summary>
    /// Реактивная базовая сущность. Поля создания и обновления реактивны для UI, 
    /// а чисто служебные флаги ОРМ и репликации оставлены обычными свойствами.
    /// </summary>
    public abstract partial class BaseEntity : ObservableObject
    {
        [ObservableProperty]
        public partial Guid Id { get; set; } = Guid.NewGuid();

        // 🎯 Чисто служебный флаг для ORM и репозиториев (0 = грязный, 1 = засинхронен)
        public int IsSynced { get; set; } = 0;

        // 🔥 ВОЗВРАЩАЕМ НА МЕСТО: Служебное поле версии для триггеров PostgreSQL!
        public long Version { get; set; } = 0;


        [ObservableProperty] public partial bool IsDeleted { get; set; } = false;


        // --- ⏳ Раздельное время создания ---

        // Когда автор физически создал запись на своем устройстве
        [ObservableProperty]
        public partial DateTimeOffset LocalCreatedAt { get; set; } = DateTimeOffset.Now;

        // When база данных (сервер) закоммитила эту запись у себя
        [ObservableProperty]
        public partial DateTimeOffset ServerCreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // --- 🔄 Единое общее время обновления ---

        [ObservableProperty]
        private DateTimeOffset _updatedAt = DateTimeOffset.UtcNow;
    }
}
