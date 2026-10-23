using MySQLManager.Database;
using MySQLManager.Database.Database_Exceptions;
using MySQLManager.Session_Details.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using Do;

namespace MySQLManager.Database.Session_Details
{
    /// <summary>
    /// Базовый адаптер SQL-запросов MySQL: выполняет команды и возвращает результаты,
    /// перехватывая ошибки и логируя их через <see cref="Do.Out"/>.
    /// </summary>
    internal class QueryAdapter : IRegularQueryAdapter
    {
        private static bool dbEnabled
        {
            get
            {
                return DatabaseManager.dbEnabled;
            }
        }
        protected MySqlClient client;
        protected MySqlCommand command;

        public DatabaseType dbType
        {
            get
            {
                return DatabaseType.MySql;
            }
        }

        /// <summary>
        /// Создаёт адаптер для указанного MySQL-клиента.
        /// </summary>
        /// <param name="client">Клиент MySQL.</param>
        internal QueryAdapter(MySqlClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Добавляет к команде BLOB-параметр с двоичными данными.
        /// </summary>
        /// <param name="name">Имя параметра.</param>
        /// <param name="data">Двоичные данные.</param>
        public void addParameter(string name, byte[] data)
        {
            this.command.Parameters.Add(new MySqlParameter(name, MySqlDbType.Blob, data.Length));
        }

        /// <summary>
        /// Добавляет к команде параметр со значением произвольного типа.
        /// </summary>
        /// <param name="parameterName">Имя параметра.</param>
        /// <param name="val">Значение параметра.</param>
        public void addParameter(string parameterName, object val)
        {
            this.command.Parameters.AddWithValue(parameterName, val);
        }

        /// <summary>
        /// Проверяет, возвращает ли запрос хотя бы одну строку.
        /// </summary>
        /// <returns>True, если есть результат; false при ошибке или отключённой БД.</returns>
        public bool findsResult()
        {
            if (!dbEnabled)
                return false;
            var now = DateTime.Now;
            var hasRows = false;
            try
            {
                using (var reader = this.command.ExecuteReader())
                {
                    hasRows = reader.HasRows;
                }
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }
            return hasRows;
        }

        /// <summary>
        /// Выполняет запрос и возвращает первое поле результата как целое число.
        /// </summary>
        /// <returns>Целочисленное значение (0 при ошибке или отключённой БД).</returns>
        public int getInteger()
        {
            if (!dbEnabled)
                return 0;
            var now = DateTime.Now;
            var result = 0;
            try
            {
                var obj2 = this.command.ExecuteScalar();
                if (obj2 != null)
                {
                    int.TryParse(obj2.ToString(), out result);
                }
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }
            return result;
        }

        /// <summary>
        /// Выполняет запрос и возвращает единственную строку результата.
        /// </summary>
        /// <returns>Объект <see cref="DataRow"/> или null, если строк не ровно одна либо ошибка.</returns>
        public DataRow getRow()
        {
            if (!dbEnabled)
                return null;
            var now = DateTime.Now;
            DataRow row = null;
            try
            {
                var dataSet = new DataSet();
                using (var adapter = new MySqlDataAdapter(this.command))
                {
                    adapter.Fill(dataSet);
                }
                if ((dataSet.Tables.Count > 0) && (dataSet.Tables[0].Rows.Count == 1))
                {
                    row = dataSet.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }
            return row;
        }

        /// <summary>
        /// Выполняет запрос и возвращает первое поле результата как строку.
        /// </summary>
        /// <returns>Строковое значение (пустая строка при ошибке или отключённой БД).</returns>
        public string getString()
        {
            if (!dbEnabled)
                return string.Empty;
            var now = DateTime.Now;
            var str = string.Empty;
            try
            {
                var obj2 = this.command.ExecuteScalar();
                if (obj2 != null)
                {
                    str = obj2.ToString();
                }
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }
            return str;
        }

        /// <summary>
        /// Выполняет запрос и возвращает весь результат в виде таблицы.
        /// </summary>
        /// <returns>Объект <see cref="DataTable"/> (пустой при ошибке или отключённой БД).</returns>
        public DataTable getTable()
        {
            var now = DateTime.Now;
            var dataTable = new DataTable();
            if (!dbEnabled)
                return dataTable;
            try
            {
                using (var adapter = new MySqlDataAdapter(this.command))
                {
                    adapter.Fill(dataTable);
                }
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }
            return dataTable;
        }

        /// <summary>
        /// Выполняет INSERT-запрос и возвращает идентификатор последней вставленной записи.
        /// </summary>
        /// <returns>LastInsertedId (0 при ошибке или отключённой БД).</returns>
        public long insertQuery()
        {
            if (!dbEnabled)
                return 0;
            var now = DateTime.Now;
            var lastInsertedId = 0L;
            try
            {
                this.command.ExecuteScalar();
                lastInsertedId = this.command.LastInsertedId;
            }
            catch (Exception ex)
                {
                LogQueryError(ex);
                }

            return lastInsertedId;
        }

        /// <summary>
        /// Выполняет SQL-запрос, автоматически выбирая способ обработки по его типу
        /// (INSERT — скалярное значение, SELECT — таблица, прочее — ExecuteNonQuery).
        /// </summary>
        /// <param name="query">Текст SQL-запроса.</param>
        /// <param name="parametersKeys">Имена параметров (необязательно).</param>
        /// <param name="parametersValues">Значения параметров (необязательно).</param>
        /// <returns>Результат выполнения запроса.</returns>
        public object query(string query, string[] parametersKeys = null, string[] parametersValues = null)
        {
            if (!dbEnabled)
                return 0;

            this.setQuery(query);
            if (parametersKeys != null)
            {
                uint count = 0;
                foreach (var Key in parametersKeys)
                {
                    this.addParameter(Key, parametersValues[count]);
                    ++count;
                }
            }

            if (query.StartsWith("INSERT"))
            {
                try
                {
                    return (long)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    LogQueryError(ex);
                    return 0;
                }
            }
            else if (query.StartsWith("SELECT"))
            {
                return getTable();
            }
            else
            {
                runQuery();
            }

            return 0;
        }

        /// <summary>
        /// Выполняет накопленную команду (ExecuteNonQuery). При ошибке откатывает
        /// активную транзакцию и пробрасывает <see cref="DatabaseException"/> наверх.
        /// </summary>
        /// <exception cref="DatabaseException">Если запрос завершился с ошибкой.</exception>
        public void runQuery()
        {
            if (!dbEnabled)
                return;
            var now = DateTime.Now;
            try
            {
                this.command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                LogQueryError(e);
                // Было: голый throw без try/catch у вызывающего кода — любая ошибка
                // SQL (например, UPDATE количества ракет в UpdateRocket()) молча
                // разрывала цепочку обработки пакета: update не выполнялся, а
                // синхронизация клиента после него не отправлялась ("купил PLT-3030,
                // в космосе не появилось").
                // Стало: пробрасываем ошибку наверх (для корректного rollback в
                // TransactionQueryReactor), но только если она НЕ из транзакции —
                // в MySQL DDL вызывает implicit commit, поэтому после ошибки внутри
                // открытой транзакции откатываем её явно, чтобы не было "тихой порчи".
                TryRollbackActiveTransaction(e);
                throw new DatabaseException("Query failed: " + this.command.CommandText, e);
            }
        }

        // Если команда выполнялась внутри MySqlTransaction, при ошибке делаем явный
        // Rollback (иначе частично выполненные запросы рискуют быть закоммиченными).
        /// <summary>
        /// Если команда выполнялась внутри MySqlTransaction, при ошибке делает явный Rollback
        /// (иначе частично выполненные запросы рискуют быть закоммиченными).
        /// </summary>
        /// <param name="cause">Исходная ошибка, вызвавшая откат.</param>
        private void TryRollbackActiveTransaction(Exception cause)
        {
            try
            {
                var tx = this.command.Transaction;
                if (tx != null)
                {
                    tx.Rollback();
                    Out.WriteLine("MySQL transaction rolled back due to error: " + cause.Message);
                }
            }
            catch (Exception rollbackError)
            {
                // Откат мог уже быть выполнен ранее (double-rollback) — логируем и
                // продолжаем пробрасывать исходную ошибку.
                Out.WriteLine("MySQL rollback failed: " + rollbackError.Message);
            }
        }

        /// <summary>
        /// Подробно логирует ошибку выполнения SQL-команды: тип и сообщение исключения,
        /// текст запроса, значения параметров (и внутренние ошибки, например у MySqlException).
        /// </summary>
        /// <param name="ex">Перехваченное исключение.</param>
        private void LogQueryError(Exception ex)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("Error [MySQL] ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                sb.AppendLine();
                sb.Append("  Query: ").Append(this.command != null ? this.command.CommandText : "<null>");
                if (this.command != null && this.command.Parameters != null && this.command.Parameters.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append("  Parameters:");
                    foreach (IDataParameter p in this.command.Parameters)
                    {
                        sb.Append(" ").Append(p.ParameterName).Append("=").Append(
                            p.Value == null ? "NULL" : Convert.ToString(p.Value, System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                // Для MySqlException дополнительно печатаем код ошибки MySQL (Number) —
                // например 1146 = "Table doesn't exist", 1054 = "Unknown column".
                var myEx = ex as MySqlException;
                if (myEx != null)
                {
                    sb.AppendLine();
                    sb.Append("  MySqlErrorCode: ").Append(myEx.Number);
                }
                // Выводим всю цепочку InnerException — без неё видно только симптом, а не причину.
                var inner = ex.InnerException;
                int depth = 1;
                while (inner != null)
                {
                    sb.AppendLine();
                    sb.Append("  Inner[").Append(depth++).Append("] ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
                    inner = inner.InnerException;
                }
                sb.AppendLine();
                sb.Append("  StackTrace:").AppendLine().Append(ex.StackTrace);
                Out.WriteLine(sb.ToString());
            }
            catch (Exception logError)
            {
                // Логирование не должно ронять сервер; на худой конец пишем минимальную строку.
                Out.WriteLine("Error [MySQL] (failed to build detailed log: " + logError.Message + ") " +
                              (this.command != null ? this.command.CommandText : "<null>"));
            }
        }

        /// <summary>
        /// Очищает параметры команды и задаёт текст SQL-запроса.
        /// </summary>
        /// <param name="query">Текст SQL-запроса.</param>
        public void setQuery(string query)
        {
            this.command.Parameters.Clear();
            this.command.CommandText = query;
        }
    }
}

