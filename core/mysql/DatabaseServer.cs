using MySQLManager.Database.Database_Exceptions;

namespace MySQLManager.Managers.Database
{
    /// <summary>
    /// Описание параметров подключения к серверу базы данных (хост, порт, учётные данные).
    /// </summary>
    public class DatabaseServer
    {
        private readonly string databaseName;
        private readonly string host;
        private readonly string password;
        private readonly uint port;
        private readonly string user;

        /// <summary>
        /// Создаёт описание сервера БД с проверкой обязательных параметров.
        /// </summary>
        /// <param name="host">Адрес хоста.</param>
        /// <param name="port">Порт сервера баз данных.</param>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="password">Пароль (может быть null — тогда пустая строка).</param>
        /// <param name="databaseName">Имя базы данных.</param>
        /// <exception cref="DatabaseException">Если не задан хост, пользователь или имя базы.</exception>
        public DatabaseServer(string host, uint port, string username, string password, string databaseName)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new DatabaseException("No host was given");
            }
            if (string.IsNullOrEmpty(username))
            {
                throw new DatabaseException("No username was given");
            }
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new DatabaseException("No database name was given");
            }
            this.host = host;
            this.port = port;
            this.databaseName = databaseName;
            user = username;
            this.password = password ?? "";
        }

        /// <summary>
        /// Возвращает имя базы данных.
        /// </summary>
        /// <returns>Имя базы данных.</returns>
        public string GetDatabaseName()
        {
            return databaseName;
        }

        /// <summary>
        /// Возвращает адрес хоста сервера баз данных.
        /// </summary>
        /// <returns>Адрес хоста.</returns>
        public string GetHost()
        {
            return host;
        }

        /// <summary>
        /// Возвращает пароль пользователя базы данных.
        /// </summary>
        /// <returns>Пароль.</returns>
        public string GetPassword()
        {
            return password;
        }

        /// <summary>
        /// Возвращает порт сервера баз данных.
        /// </summary>
        /// <returns>Номер порта.</returns>
        public uint GetPort()
        {
            return port;
        }

        /// <summary>
        /// Возвращает имя пользователя базы данных.
        /// </summary>
        /// <returns>Имя пользователя.</returns>
        public string GetUsername()
        {
            return user;
        }

        /// <summary>
        /// Возвращает текстовое представление в виде "user@host".
        /// </summary>
        /// <returns>Строковое описание подключения.</returns>
        public override string ToString()
        {
            return (user + "@" + host);
        }
    }
}