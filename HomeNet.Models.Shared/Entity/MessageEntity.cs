using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace HomeNet.Models.Shared.Entity
{
    public partial class MessageEntity : BaseEntity
    {
        [ObservableProperty]
        public partial Guid SenderId { get; set; }
        [ObservableProperty]
        public partial Guid ReceiverId { get; set; }

        [ObservableProperty] public partial string? Text { get; set; } = null;

        [ObservableProperty]
        public partial string MediaType { get; set; } = "Text";

        [ObservableProperty]
        public partial string? FilePath { get; set; }

        [ObservableProperty]
        public partial bool IsRead { get; set; }

        [field: NotMapped]
        [ObservableProperty]
        public partial bool IsOutgoing { get; set; }

        [NotMapped]
        public string FormattedTime => LocalCreatedAt.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}
