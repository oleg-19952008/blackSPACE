namespace MySQLManager.Database.Session_Details.Interfaces
{
    using MySQLManager.Session_Details.Interfaces;
    using System;

    /// <summary>
    /// Расширенный адаптер запросов с поддержкой транзакций (коммит/откат).
    /// </summary>
    public interface IQueryAdapter : IRegularQueryAdapter, IDisposable
    {
        /// <summary>
        /// Фиксирует текущую транзакцию.
        /// </summary>
        void doCommit();
        /// <summary>
        /// Откатывает текущую транзакцию.
        /// </summary>
        void doRollBack();
        /// <summary>
        /// Выполняет накопленный запрос (или транзакцию запросов).
        /// </summary>
        void runQuery();
    }
}

