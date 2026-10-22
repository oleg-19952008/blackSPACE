namespace MySQLManager.Database
{
    using MySQLManager.Database.Database_Exceptions;
    using MySQLManager.Database.Session_Details.Interfaces;
    using MySQLManager.Managers.Database;
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Text;
    using System.Collections;
    using System.Data.SqlClient;

    /// <summary>
    /// Менеджер базы данных: пул клиентов (MySQL/MS SQL), строка подключения и выдача реакторов запросов.
    /// </summary>
    public class DatabaseManager
    {
        private int beginClientAmount;
        private string connectionString;
        private List<MySqlClient> databaseClients;
        private bool isConnected = false;
        private uint maxPoolSize;
        private DatabaseServer server;
        private Queue connections;
        internal DatabaseType type { get; set; }

        /// <summary>
        /// Глобальный переключатель: выполнять ли запросы к базе данных вообще.
        /// </summary>
        public static bool dbEnabled = true;

        /// <summary>
        /// Создаёт менеджер с указанным размером пула и типом СУБД.
        /// </summary>
        /// <param name="maxPoolSize">Максимальный размер пула соединений.</param>
        /// <param name="clientAmount">Начальное количество клиентов.</param>
        /// <param name="dbType">Тип базы данных.</param>
        /// <exception cref="DatabaseException">Если начальные клиенты превышают размер пула.</exception>
        public DatabaseManager(uint maxPoolSize, int clientAmount, DatabaseType dbType)
        {
            if (maxPoolSize < clientAmount)
                throw new DatabaseException("The poolsize can not be larger than the client amount!");

            this.type = dbType;
            this.beginClientAmount = clientAmount;
            this.maxPoolSize = maxPoolSize;
            this.connections = new Queue();
        }

        /// <summary>
        /// Создаёт, подключает и добавляет в список новый MySQL-клиент.
        /// </summary>
        /// <param name="id">Идентификатор соединения.</param>
        private void addConnection(int id)
        {
            var item = new MySqlClient(this, id);
            item.connect();
            this.databaseClients.Add(item);
        }

        /// <summary>
        /// Формирует строку подключения в зависимости от типа СУБД (MySQL или MS SQL).
        /// </summary>
        private void createNewConnectionString()
        {
            if (this.type == DatabaseType.MySql)
            {
                var connectionString = new MySqlConnectionStringBuilder
                {
                    Server = this.server.GetHost(),
                    Port = this.server.GetPort(),
                    UserID = this.server.GetUsername(),
                    Password = this.server.GetPassword(),
                    Database = this.server.GetDatabaseName(),
                    MinimumPoolSize = this.maxPoolSize / 2,
                    MaximumPoolSize = this.maxPoolSize,
                    AllowZeroDateTime = true,
                    ConvertZeroDateTime = true,
                    DefaultCommandTimeout = 300,
                    ConnectionTimeout = 10
                };

                this.setConnectionString(connectionString.ToString());
            }
            else
            {
                var connectionString = new SqlConnectionStringBuilder
                {
                    DataSource = this.server.GetHost(),
                    //Port = this.server.getPort(),
                    UserID = this.server.GetUsername(),
                    Password = this.server.GetPassword(),
                    InitialCatalog = this.server.GetDatabaseName(),
                    MinPoolSize = (int)this.maxPoolSize / 2,
                    MaxPoolSize = (int)this.maxPoolSize,
                    //AllowZeroDateTime = true,
                    //ConvertZeroDateTime = true,
                    //DefaultCommandTimeout = 300,
                    ConnectTimeout = 10,
                    Pooling = true
                };

                this.setConnectionString(connectionString.ToString());
            }
        }

        /// <summary>
        /// Останавливает менеджер: освобождает и отключает все клиенты, очищает список.
        /// </summary>
        public void destroy()
        {
            lock (this)
            {
                this.isConnected = false;
                if (this.databaseClients != null)
                {
                    foreach (var client in this.databaseClients)
                    {
                        if (!client.isAvailable())
                        {
                            client.Dispose();
                        }
                        client.disconnect();
                 
       
                    }
                    this.databaseClients.Clear();
                }
            }
        }

        /// <summary>
        /// Отключает свободные (незанятые) клиенты пула.
        /// </summary>
        private void disconnectUnusedClients()
        {
            lock (this)
            {
                foreach (var client in this.databaseClients)
                {
                    if (client.isAvailable())
                    {
                        client.disconnect();
                    }
                }
            }
        }

        /// <summary>
        /// Возвращает текущую строку подключения.
        /// </summary>
        /// <returns>Строка подключения.</returns>
        internal string getConnectionString()
        {
            return this.connectionString;
        }

        /// <summary>
        /// Выдаёт реактор запросов: берёт свободный клиент из пула либо создаёт новый.
        /// </summary>
        /// <returns>Экземпляр <see cref="IQueryAdapter"/>.</returns>
        public IQueryAdapter getQueryreactor()
        {
            IDatabaseClient dbClient = null;
            lock (connections.SyncRoot)
            {
                if (connections.Count > 0)
                {
                    dbClient = (IDatabaseClient)connections.Dequeue();
                }
            }

            if (dbClient != null)
            {
                dbClient.connect();
                dbClient.prepare();
                return dbClient.getQueryReactor();
            }
            else
            {
        
                    IDatabaseClient connection = new MySqlClient(this, 0);
                    connection.connect();
                    connection.prepare();
                    return connection.getQueryReactor();
              
                //else
                //{
                //    IDatabaseClient connection = new MsSQLClient(this, 0);
                //    connection.connect();
                //    connection.prepare();
              //   return connection.getQueryReactor();
                //}
            }
        }

        /// <summary>
        /// Возвращает освободившийся клиент обратно в пул соединений.
        /// </summary>
        /// <param name="dbClient">Освобождённый клиент БД.</param>
        internal void FreeConnection(IDatabaseClient dbClient)
        {
            lock (connections.SyncRoot)
            {
                connections.Enqueue(dbClient);
            }
        }

        /// <summary>
        /// Инициализирует менеджер: строит строку подключения и список клиентов.
        /// </summary>
        /// <exception cref="Exception">Если не удалось подключить клиентов к базе.</exception>
        public void init()
        {
            try
            {
                this.createNewConnectionString();
                this.databaseClients = new List<MySqlClient>((int) this.maxPoolSize);
            }
            catch (MySqlException exception)
            {
                this.isConnected = false;
                throw new Exception("Could not connect the clients to the database: " + exception.Message);
            }
            this.isConnected = true;
        }

        /// <summary>
        /// Проверяет, инициализировано ли соединение с базой данных.
        /// </summary>
        /// <returns>True, если менеджер подключён.</returns>
        public bool isConnectedToDatabase()
        {
            return this.isConnected;
        }

        /// <summary>
        /// Сохраняет строку подключения.
        /// </summary>
        /// <param name="connectionString">Новая строка подключения.</param>
        private void setConnectionString(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Задаёт параметры сервера базы данных.
        /// </summary>
        /// <param name="host">Адрес хоста.</param>
        /// <param name="port">Порт сервера.</param>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="password">Пароль.</param>
        /// <param name="databaseName">Имя базы данных.</param>
        /// <returns>True, если параметры корректны; иначе false.</returns>
        public bool setServerDetails(string host, uint port, string username, string password, string databaseName)
        {
            try
            {
                this.server = new DatabaseServer(host, port, username, password, databaseName);
                return true;
            }
            catch (DatabaseException)
            {
                this.isConnected = false;
                return false;
            }
        }
    }
}

