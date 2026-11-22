namespace HomeNetOrm.Exceptions
{
    public class DatabaseException(string message, Exception? inner = null) : Exception(message, inner);
}
