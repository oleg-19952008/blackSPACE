namespace MySQLManager.Database.Session_Details
{
    using MySQLManager.Database;
    using MySQLManager.Database.Database_Exceptions;
    using MySQLManager.Database.Session_Details.Interfaces;
    using MySQLManager.Session_Details.Interfaces;
    using System;

    /// <summary>
    /// Обычный (нетранзакционный) реактор запросов MySQL: каждый запрос выполняется автоматически.
    /// </summary>
    internal class NormalQueryReactor : QueryAdapter, IQueryAdapter, IRegularQueryAdapter, IDisposable
    {
        /// <summary>
        /// Создаёт реактор для указанного MySQL-клиента.
        /// </summary>
        /// <param name="client">Клиент MySQL.</param>
        internal NormalQueryReactor(MySqlClient client) : base(client)
        {
            base.command = client.getNewCommand();
        }

        /// <summary>
        /// Освобождает команду и сообщает клиенту о завершении работы.
        /// </summary>
        public void Dispose()
        {
            base.command.Dispose();
            base.client.reportDone();
        }

        /// <summary>
        /// Недопустимо для нетранзакционного реактора.
        /// </summary>
        public void doCommit()
        {
            new TransactionException("Can't use rollback on a non-transactional Query reactor");
        }

        /// <summary>
        /// Недопустимо для нетранзакционного реактора.
        /// </summary>
        public void doRollBack()
        {
            new TransactionException("Can't use rollback on a non-transactional Query reactor");
        }

        /// <summary>
        /// Указывает, что запросы выполняются в режиме авто-коммита.
        /// </summary>
        /// <returns>Всегда true.</returns>
        internal bool getAutoCommit()
        {
            return true;
        }
    }
}

