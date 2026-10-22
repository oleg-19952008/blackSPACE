using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Do.core.mysql;
using MySQLManager.Database.Database_Exceptions;
using MySQLManager.Session_Details.Interfaces;

namespace MySQLManager.Database.Session_Details.Interfaces
{
	/// <summary>
	/// Обычный (нетранзакционный) реактор запросов MS SQL.
	/// </summary>
	class MsSqlQueryReactor : MssqlQueryAdapter, IQueryAdapter, IRegularQueryAdapter, IDisposable
	{
        /// <summary>
        /// Создаёт реактор для указанного MS SQL-клиента.
        /// </summary>
        /// <param name="client">Клиент MS SQL.</param>
        internal MsSqlQueryReactor(MsSQLClient client)
            : base(client)
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
