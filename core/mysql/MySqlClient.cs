using System;
using MySQLManager.Database.Session_Details;
using MySQLManager.Database.Session_Details.Interfaces;
using MySql.Data.MySqlClient;

namespace MySQLManager.Database
{
    /// <summary>
    /// Клиент MySQL-соединения с базой данных, управляемый пулом <see cref="DatabaseManager"/>.
    /// </summary>
    public class MySqlClient : IDatabaseClient
    {
        private MySqlConnection connection;
        private DatabaseManager dbManager;
        private IQueryAdapter info;

        /// <summary>
        /// Создаёт клиент MySQL с соединением, построенным по строке подключения менеджера.
        /// </summary>
        /// <param name="dbManager">Менеджер базы данных (пул соединений).</param>
        /// <param name="id">Идентификатор соединения в пуле.</param>
        public MySqlClient(DatabaseManager dbManager, int id)
        {
            this.dbManager = dbManager;
            this.connection = new MySqlConnection(dbManager.getConnectionString());
        }

        /// <summary>
        /// Открывает соединение с MySQL; ошибки подключения подавляются.
        /// </summary>
        public void connect()
        {
            try
            {
                this.connection.Open();
            }
            catch (MySqlException mysqlex)
            {
             //   Console.WriteLine("Can't connect to MySql");
              //  Console.WriteLine(mysqlex.StackTrace);
           //     System.IO.File.WriteAllText("errorlog.txt", "Error Log (" + DateTime.Now + "):" + Environment.NewLine + mysqlex.StackTrace);
         //       Console.ReadKey();
           //     Environment.Exit(-1);
            }
            catch (Exception e)
            {
             ///   System.IO.File.WriteAllText("errorlog.txt", "Error Log (" + DateTime.Now + "):" + Environment.NewLine + e.StackTrace);

            }
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
        /// Создаёт новую MySQL-команду для текущего соединения.
        /// </summary>
        /// <returns>Экземпляр <see cref="MySqlCommand"/>.</returns>
        internal MySqlCommand getNewCommand()
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
        /// Начинает новую транзакцию MySQL на текущем соединении.
        /// </summary>
        /// <returns>Экземпляр <see cref="MySqlTransaction"/>.</returns>
        internal MySqlTransaction getTransaction()
        {
            return this.connection.BeginTransaction();
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
        /// Подготавливает клиента к работе, создавая обычный (нетранзакционный) реактор запросов.
        /// </summary>
        public void prepare()
        {
            this.info = new NormalQueryReactor(this);
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

