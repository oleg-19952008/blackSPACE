namespace MySQLManager.Session_Details.Interfaces
{
    using System;
    using System.Data;
    using MySQLManager.Database;

    /// <summary>
    /// Базовый интерфейс адаптера регулярных (нетранзакционных) запросов к базе данных.
    /// </summary>
    public interface IRegularQueryAdapter
    {
        /// <summary>
        /// Добавляет параметр с указанным именем к текущему запросу.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="query">Значение параметра.</param>
        void addParameter(string name, object query);
        /// <summary>
        /// Проверяет, содержит ли результат запроса хотя бы одну строку.
        /// </summary>
        /// <returns>True, если результат найден.</returns>
        bool findsResult();
        /// <summary>
        /// Возвращает первое поле результата запроса как целое число.
        /// </summary>
        /// <returns>Целочисленное значение.</returns>
        int getInteger();
        /// <summary>
        /// Возвращает первую строку результата запроса.
        /// </summary>
        /// <returns>Объект <see cref="DataRow"/>.</returns>
        DataRow getRow();
        /// <summary>
        /// Возвращает первое поле результата запроса как строку.
        /// </summary>
        /// <returns>Строковое значение.</returns>
        string getString();
        /// <summary>
        /// Возвращает весь результат запроса в виде таблицы.
        /// </summary>
        /// <returns>Объект <see cref="DataTable"/>.</returns>
        DataTable getTable();
        /// <summary>
        /// Выполняет SQL-запрос (опционально с параметрами) и возвращает результат.
        /// </summary>
        /// <param name="query">Текст SQL-запроса.</param>
        /// <param name="parametersKeys">Имена параметров (для подготовленных запросов).</param>
        /// <param name="parametersValues">Значения параметров.</param>
        /// <returns>Результат выполнения запроса.</returns>
        object query(string query, string[] parametersKeys = null, string[] parametersValues = null);
        /// <summary>
        /// Задаёт SQL-запрос для последующего выполнения.
        /// </summary>
        /// <param name="query">Текст SQL-запроса.</param>
        void setQuery(string query);
        /// <summary>
        /// Тип базы данных, к которой обращён адаптер.
        /// </summary>
        DatabaseType dbType { get; }
    }
}

