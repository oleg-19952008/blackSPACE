using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Do.Core
{
    class ParserPacket : IDisposable
    {
        private readonly string[] _packetData;
        private int _count = -1;

        public ParserPacket(string packet)
        {
            _packetData = packet.Split('|');
        }

        public int GetInt()
        {
            _count++;
            return Convert.ToInt32(_packetData[_count]);
        }

        public short GetShort()
        {
            _count++;
            return Convert.ToInt16(_packetData[_count]);
        }

        public uint GetUInt()
        {
            _count++;
            return Convert.ToUInt32(_packetData[_count]);
        }

        public ulong GetULong()
        {
            _count++;
            return Convert.ToUInt64(_packetData[_count]);
        }

        public ushort GetUShort()
        {
            _count++;
            return Convert.ToUInt16(_packetData[_count]);
        }

        public string GetString()
        {
            _count++;
            return _packetData[_count];
        }

        public bool GetBool()
        {
            _count++;
            return Program.ToBool(_packetData[_count]);
        }

        public bool MoreToRead
        {
            get { return (_packetData.Count() > (_count + 1)) ? true : false; }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
