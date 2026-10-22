namespace MySQLManager.Database.Database_Exceptions
{
    using System;

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message)
        {
        }

        // Переопределение с inner exception: используется в QueryAdapter.runQuery(),
        // чтобы пробрасывать ошибку SQL наверх (для корректного rollback транзакций),
        // не теряя исходную MySqlException.
        public DatabaseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

