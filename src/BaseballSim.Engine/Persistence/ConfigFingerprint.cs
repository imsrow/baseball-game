using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using BaseballSim.Engine.Config;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// LeagueConfig 전체 값의 지문(FNV-1a 64비트). 저장 당시와 설정이 다르면 결과 재현이 달라질 수 있어 경고에 쓴다.
    /// </summary>
    public static class ConfigFingerprint
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static ulong Compute(LeagueConfig config)
        {
            var text = new StringBuilder();
            Append(text, config, 0);
            ulong hash = FnvOffset;
            foreach (byte b in Encoding.UTF8.GetBytes(text.ToString()))
            {
                hash = unchecked((hash ^ b) * FnvPrime);
            }

            return hash;
        }

        // 설정 클래스 중첩 깊이 안전장치
        private const int MaxDepth = 8;

        private static void Append(StringBuilder text, object value, int depth)
        {
            if (value == null)
            {
                text.Append("null;");
                return;
            }

            Type type = value.GetType();
            if (value is string || type.IsPrimitive || type.IsEnum)
            {
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append(';');
                return;
            }

            if (value is IEnumerable sequence)
            {
                text.Append('[');
                foreach (object item in sequence)
                {
                    Append(text, item, depth + 1);
                }

                text.Append(']');
                return;
            }

            if (depth > MaxDepth)
            {
                return;
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                text.Append(property.Name).Append('=');
                Append(text, property.GetValue(value), depth + 1);
            }
        }
    }
}
