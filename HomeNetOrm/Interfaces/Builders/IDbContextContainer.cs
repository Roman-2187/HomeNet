namespace HomeNetOrm.Interfaces.Builders
{
    public interface IDbContextContainer
    {
        Task InitializeAllDatabasesAsync();
       
       

        /// <summary>
        /// Вложенный рекорд сигнала готовности Хаба
        /// </summary>
        public record InfrastructureReady();


    }
}
