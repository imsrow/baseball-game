using System.Collections.Generic;
using System.IO;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 저장용 기본형 읽기 도우미
    /// </summary>
    public sealed class SaveReader
    {
        private readonly BinaryReader _reader;

        public SaveReader(BinaryReader reader)
        {
            _reader = reader;
        }

        public int Int() => _reader.ReadInt32();

        public ulong ULong() => _reader.ReadUInt64();

        public double Double() => _reader.ReadDouble();

        public bool Bool() => _reader.ReadBoolean();

        public string String() => _reader.ReadString();

        public double? NullableDouble() => Bool() ? Double() : (double?)null;

        public int? NullableInt() => Bool() ? Int() : (int?)null;

        public List<int> IntList()
        {
            int count = Int();
            var list = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(Int());
            }

            return list;
        }
    }
}
