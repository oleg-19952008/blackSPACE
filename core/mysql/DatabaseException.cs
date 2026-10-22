namespace MySQLManager.Database.Database_Exceptions
{
    using System;

    /// <summary>
    /// Исключение уровня базы данных, пробрасываемое при ошибках выполнения SQL-запросов.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// Создаёт исключение с указанным сообщением.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        public DatabaseException(string message) : base(message)
        {
        }

        // Переопределение с inner exception: используется в QueryAdapter.runQuery(),
        // чтобы пробрасывать ошибку SQL наверх (для корректного rollback транзакций),
        // не теряя исходную MySqlException.
        /// <summary>
        /// Создаёт исключение с сообщением и внутренним исключением (сохраняет исходную ошибку СУБД).
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="inner">Исходное исключение.</param>
        public DatabaseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

