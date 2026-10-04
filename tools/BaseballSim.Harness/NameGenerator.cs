using System.Collections.Generic;
using BaseballSim.Engine.Randomness;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 가상 리그 이름 생성 (실제 구단·선수명 사용 안 함)
    /// </summary>
    public sealed class NameGenerator
    {
        private static readonly string[] Cities =
        {
            "은하", "청운", "해솔", "백야", "금빛", "새봄", "푸른들", "한울", "다온", "누리", "별빛", "아라",
        };

        private static readonly string[] Nicknames =
        {
            "레이븐스", "코메츠", "타이드", "울브스", "스톰", "팰컨스", "오로라", "썬더볼츠", "아울스", "바이슨스", "세일러스", "링스",
        };

        private static readonly string[] Surnames =
        {
            "김", "이", "박", "최", "정", "강", "조", "윤", "장", "임", "한", "오", "서", "신", "권", "황", "안", "송", "류", "홍",
        };

        private static readonly string[] Syllables =
        {
            "가", "건", "결", "다", "도", "라", "란", "루", "민", "별", "빈", "새", "서", "솔", "수", "아", "언", "온", "우",
            "윤", "율", "은", "이", "재", "준", "지", "진", "찬", "태", "하", "한", "해", "현", "호", "환", "후", "휘",
        };

        private readonly IRandomSource _random;
        private readonly HashSet<string> _used = new HashSet<string>();

        public NameGenerator(IRandomSource random)
        {
            _random = random;
        }

        public string TeamName(int index)
        {
            string city = Cities[index % Cities.Length];
            string nickname = Nicknames[(index * 5 + index / Nicknames.Length) % Nicknames.Length];
            string suffix = index >= Cities.Length ? " " + (index / Cities.Length + 1) : string.Empty;
            return city + " " + nickname + suffix;
        }

        public string PlayerName()
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                string name = Surnames[_random.NextInt(Surnames.Length)]
                    + Syllables[_random.NextInt(Syllables.Length)]
                    + Syllables[_random.NextInt(Syllables.Length)];
                if (_used.Add(name))
                {
                    return name;
                }
            }

            return "선수" + _used.Count;
        }
    }
}
