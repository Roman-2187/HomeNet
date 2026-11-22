namespace HomeNetOrm.Exceptions
{
    public class DuplicateMessageException(Guid messageId)
        : Exception($"Сообщение с ID {messageId} уже существует в локальной базе.")
    {
        public Guid MessageId { get; } = messageId;
    }
}