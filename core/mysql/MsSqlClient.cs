using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySQLManager.Database.Session_Details.Interfaces;
using System.Data.SqlClient;
using MySQLManager.Database.Session_Details;

namespace MySQLManager.Database
{
    /// <summary>
    /// Клиент MS SQL Server-соединения с базой данных, управляемый пулом <see cref="DatabaseManager"/>.
    /// </summary>
    public class MsSQLClient : IDatabaseClient
    {
        private SqlConnection connection;
        private DatabaseManager dbManager;
        private IQueryAdapter info;

        /// <summary>
        /// Создаёт клиент MS SQL с соединением, построенным по строке подключения менеджера.
        /// </summary>
        /// <param name="dbManager">Менеджер базы данных (пул соединений).</param>
        /// <param name="id">Идентификатор соединения в пуле.</param>
        public MsSQLClient(DatabaseManager dbManager, int id)
        {
            this.dbManager = dbManager;
            this.connection = new SqlConnection(dbManager.getConnectionString());
        }

        /// <summary>
        /// Открывает соединение с сервером MS SQL.
        /// </summary>
        public void connect()
        {
            this.connection.Open();
        }

        /// <summary>
        /// Закрывает соединение с базой данных.
        /// </summary>
        public void disconnect()
        {
            try
            {
                this.connection.Close();
            }
            catch
            { }
        }

        /// <summary>
        /// Освобождает ресурсы: закрывает соединение и возвращает клиент в пул менеджера.
        /// </summary>
        public void Dispose()
        {
            this.info = null;
            disconnect();
            dbManager.FreeConnection(this);
        }

        /// <summary>
        /// Создаёт новую SQL-команду для текущего соединения.
        /// </summary>
        /// <returns>Экземпляр <see cref="SqlCommand"/>.</returns>
        internal SqlCommand getNewCommand()
        {
            return this.connection.CreateCommand();
        }

        /// <summary>
        /// Возвращает текущий адаптер (реактор) запросов клиента.
        /// </summary>
        /// <returns>Экземпляр <see cref="IQueryAdapter"/>.</returns>
        public IQueryAdapter getQueryReactor()
        {
            return this.info;
        }

        /// <summary>
        /// Проверяет, свободен ли клиент (не имеет занятого реактора запросов).
        /// </summary>
        /// <returns>True, если соединение доступно.</returns>
        public bool isAvailable()
        {
            return (this.info == null);
        }

        /// <summary>
        /// Подготавливает клиента к работе, создавая обычный реактор запросов MS SQL.
        /// </summary>
        public void prepare()
        {
            this.info = new MsSqlQueryReactor(this);
        }

        /// <summary>
        /// Сообщает менеджеру о завершении работы (освобождает клиент).
        /// </summary>
        public void reportDone()
        {
            this.Dispose();
        }
    }
}
