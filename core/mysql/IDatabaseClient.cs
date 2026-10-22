using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MySQLManager.Database.Session_Details.Interfaces
{
	/// <summary>
	/// Интерфейс клиента базы данных: подключение, получение реактора запросов и служебные операции.
	/// </summary>
	interface IDatabaseClient : IDisposable
	{
        /// <summary>
        /// Устанавливает соединение с базой данных.
        /// </summary>
        void connect();
        /// <summary>
        /// Разрывает соединение с базой данных.
        /// </summary>
        void disconnect();
        //void Dispose();
        /// <summary>
        /// Возвращает адаптер (реактор) для выполнения запросов к этой базе.
        /// </summary>
        /// <returns>Экземпляр <see cref="IQueryAdapter"/>.</returns>
        IQueryAdapter getQueryReactor();
        /// <summary>
        /// Проверяет доступность соединения с базой данных.
        /// </summary>
        /// <returns>True, если соединение доступно.</returns>
        bool isAvailable();
        /// <summary>
        /// Подготавливает соединение к использованию.
        /// </summary>
        void prepare();
        /// <summary>
        /// Сообщает менеджеру о завершении работы с клиентом (возврат в пул).
        /// </summary>
        void reportDone();
	}
}
