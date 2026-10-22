using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do.Core
{
    /// <summary>
    /// Разборчик текстовых сетевых пакетов, поля которых разделены символом '|'.
    /// Позволяет последовательно считывать поля пакета в типизированные значения.
    /// </summary>
    class ParserPacket : IDisposable
    {
        private readonly string[] _packetData;
        private int _count = -1;

        /// <summary>
        /// Создаёт новый экземпляр разборщика и разделяет пакет на поля.
        /// </summary>
        /// <param name="packet">Исходная строка пакета с полями через '|'.</param>
        public ParserPacket(string packet)
        {
            _packetData = packet.Split('|');
        }

        /// <summary>
        /// Считывает следующее поле пакета как целое число (int).
        /// </summary>
        /// <returns>Значение поля.</returns>
        public int GetInt()
        {
            _count++;
            return Convert.ToInt32(_packetData[_count]);
        }

        /// <summary>
        /// Считывает следующее поле пакета как короткое целое число (short).
        /// </summary>
        /// <returns>Значение поля.</returns>
        public short GetShort()
        {
            _count++;
            return Convert.ToInt16(_packetData[_count]);
        }

        /// <summary>
        /// Считывает следующее поле пакета как беззнаковое целое число (uint).
        /// </summary>
        /// <returns>Значение поля.</returns>
        public uint GetUInt()
        {
            _count++;
            return Convert.ToUInt32(_packetData[_count]);
        }

        /// <summary>
        /// Считывает следующее поле пакета как беззнаковое длинное целое число (ulong).
        /// </summary>
        /// <returns>Значение поля.</returns>
        public ulong GetULong()
        {
            _count++;
            return Convert.ToUInt64(_packetData[_count]);
        }

        /// <summary>
        /// Считывает следующее поле пакета как беззнаковое короткое целое число (ushort).
        /// </summary>
        /// <returns>Значение поля.</returns>
        public ushort GetUShort()
        {
            _count++;
            return Convert.ToUInt16(_packetData[_count]);
        }

        /// <summary>
        /// Считывает следующее поле пакета как строку.
        /// </summary>
        /// <returns>Значение поля.</returns>
        public string GetString()
        {
            _count++;
            return _packetData[_count];
        }

        /// <summary>
        /// Считывает следующее поле пакета как логическое значение.
        /// </summary>
        /// <returns>Значение поля.</returns>
        public bool GetBool()
        {
            _count++;
            return Program.ToBool(_packetData[_count]);
        }

        /// <summary>
        /// Указывает, остались ли в пакете непрочитанные поля.
        /// </summary>
        public bool MoreToRead
        {
            get { return (_packetData.Count() > (_count + 1)) ? true : false; }
        }

        /// <summary>
        /// Освобождает ресурсы объекта (подавляет финализатор).
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
