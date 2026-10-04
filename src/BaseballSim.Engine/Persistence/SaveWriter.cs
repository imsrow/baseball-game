using System.Collections.Generic;
using System.IO;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 저장용 기본형 쓰기 도우미
    /// </summary>
    public sealed class SaveWriter
    {
        private readonly BinaryWriter _writer;

        public SaveWriter(BinaryWriter writer)
        {
            _writer = writer;
        }

        public void Int(int value) => _writer.Write(value);

        public void ULong(ulong value) => _writer.Write(value);

        public void Double(double value) => _writer.Write(value);

        public void Bool(bool value) => _writer.Write(value);

        public void String(string value) => _writer.Write(value ?? string.Empty);

        public void NullableDouble(double? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                Double(value.Value);
            }
        }

        public void NullableInt(int? value)
        {
            Bool(value.HasValue);
            if (value.HasValue)
            {
                Int(value.Value);
            }
        }

        public void IntList(IList<int> values)
        {
            Int(values.Count);
            foreach (int v in values)
            {
                Int(v);
            }
        }
    }
}
