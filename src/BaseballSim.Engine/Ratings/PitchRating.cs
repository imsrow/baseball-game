using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Ratings
{
    /// <summary>
    /// 레퍼토리 내 구종 하나의 능력치
    /// </summary>
    public sealed class PitchRating
    {
        public PitchRating()
        {
        }

        public PitchRating(PitchType type, int stuff, double usage)
        {
            Type = type;
            Stuff = stuff;
            Usage = usage;
        }

        public PitchType Type { get; set; }

        /// <summary>구위(무브먼트): 헛스윙 유도, 약한 타구 유도</summary>
        public int Stuff { get; set; } = ScoutScale.Average;

        /// <summary>기본 구사 비중 (가중치, 합이 1일 필요 없음)</summary>
        public double Usage { get; set; } = 1.0;
    }
}
