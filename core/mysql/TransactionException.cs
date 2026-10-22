namespace MySQLManager.Database.Database_Exceptions
{
    using System;

    /// <summary>
    /// Исключение, возникающее при ошибках управления транзакциями базы данных.
    /// </summary>
    public class TransactionException : Exception
    {
        /// <summary>
        /// Создаёт исключение с указанным сообщением.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        public TransactionException(string message) : base(message)
        {
        }
    }
}

