namespace MySQLManager.Database.Session_Details
{
    using MySQLManager.Database;
    using MySQLManager.Database.Database_Exceptions;
    using MySQLManager.Database.Session_Details.Interfaces;
    using MySQLManager.Session_Details.Interfaces;
    using MySql.Data.MySqlClient;
    using System;

    /// <summary>
    /// Транзакционный реактор запросов MySQL: все запросы выполняются в рамках одной транзакции,
    /// которую необходимо завершить коммитом или откатом перед освобождением.
    /// </summary>
    internal class TransactionQueryReactor : QueryAdapter, IQueryAdapter, IRegularQueryAdapter, IDisposable
    {
        private bool finishedTransaction;
        private MySqlTransaction transaction;

        /// <summary>
        /// Создаёт реактор и начинает транзакцию для указанного MySQL-клиента.
        /// </summary>
        /// <param name="client">Клиент MySQL.</param>
        internal TransactionQueryReactor(MySqlClient client) : base(client)
        {
            this.initTransaction();
        }

        /// <summary>
        /// Освобождает ресурсы; выбрасывает исключение, если транзакция не была завершена.
        /// </summary>
        /// <exception cref="TransactionException">Если транзакция не подтверждена и не откачена.</exception>
        public void Dispose()
        {
            if (!this.finishedTransaction)
            {
                throw new TransactionException("The transaction needs to be completed by commit() or rollback() before you can dispose this item.");
            }
            base.command.Dispose();
            base.client.reportDone();
        }

        /// <summary>
        /// Подтверждает (фиксирует) транзакцию.
        /// </summary>
        /// <exception cref="TransactionException">При ошибке фиксации.</exception>
        public void doCommit()
        {
            try
            {
                this.transaction.Commit();
                this.finishedTransaction = true;
            }
            catch (MySqlException exception)
            {
                throw new TransactionException(exception.Message);
            }
        }

        /// <summary>
        /// Откатывает транзакцию.
        /// </summary>
        /// <exception cref="TransactionException">При ошибке отката.</exception>
        public void doRollBack()
        {
            try
            {
                this.transaction.Rollback();
                this.finishedTransaction = true;
            }
            catch (MySqlException exception)
            {
                throw new TransactionException(exception.Message);
            }
        }

        /// <summary>
        /// Указывает, что авто-коммит выключен (запросы идут в транзакцию).
        /// </summary>
        /// <returns>Всегда false.</returns>
        internal bool getAutoCommit()
        {
            return false;
        }

        /// <summary>
        /// Инициализирует команду и транзакцию клиента, связывает их между собой.
        /// </summary>
        private void initTransaction()
        {
            base.command = base.client.getNewCommand();
            this.transaction = base.client.getTransaction();
            base.command.Transaction = this.transaction;
            base.command.Connection = this.transaction.Connection;
            this.finishedTransaction = false;
        }
    }
}

