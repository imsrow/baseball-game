using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Ratings
{
    /// <summary>
    /// 야수 능력치 (20~80).
    /// 수비 도구는 포지션 공통이고, 포지션별 숙련도와 곱해 실제 수비력이 된다.
    /// </summary>
    public sealed class FielderRatings
    {
        /// <summary>숙련도 미지정을 뜻하는 값 (Config의 기본 숙련도 사용)</summary>
        public const int Unrated = 0;

        /// <summary>수비범위</summary>
        public int Range { get; set; } = ScoutScale.Average;

        /// <summary>포구</summary>
        public int Hands { get; set; } = ScoutScale.Average;

        /// <summary>송구 강도</summary>
        public int ArmStrength { get; set; } = ScoutScale.Average;

        /// <summary>송구 정확도</summary>
        public int ArmAccuracy { get; set; } = ScoutScale.Average;

        /// <summary>포수 블로킹 (폭투·포일 억제)</summary>
        public int Blocking { get; set; } = ScoutScale.Average;

        /// <summary>포수 프레이밍 (Shadow 구역 루킹 판정)</summary>
        public int Framing { get; set; } = ScoutScale.Average;

        /// <summary>포지션별 숙련도. 인덱스는 Position 값, 0이면 미지정</summary>
        public int[] Proficiency { get; set; } = new int[PositionInfo.Count];

        public int GetProficiency(Position position)
        {
            return Proficiency[(int)position];
        }

        public void SetProficiency(Position position, int value)
        {
            Proficiency[(int)position] = value;
        }
    }
}
