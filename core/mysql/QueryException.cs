namespace MySQLManager.Database.Database_Exceptions
{
    using System;

    /// <summary>
    /// Исключение с сохранением текста SQL-запроса, на котором произошла ошибка.
    /// </summary>
    public class QueryException : Exception
    {
        private string query;

        /// <summary>
        /// Создаёт исключение с описанием ошибки и текстом проблемного запроса.
        /// </summary>
        /// <param name="message">Текст ошибки.</param>
        /// <param name="query">SQL-запрос, вызвавший ошибку.</param>
        public QueryException(string message, string query) : base(message)
        {
            this.query = query;
        }

        /// <summary>
        /// Возвращает текст запроса, вызвавшего ошибку.
        /// </summary>
        /// <returns>SQL-запрос.</returns>
        public string getQuery()
        {
            return this.query;
        }
    }
}

