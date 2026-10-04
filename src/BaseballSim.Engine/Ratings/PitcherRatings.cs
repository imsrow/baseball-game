using System.Collections.Generic;
using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Ratings
{
    /// <summary>
    /// 투수 능력치 (20~80)
    /// </summary>
    public sealed class PitcherRatings
    {
        /// <summary>구속: 포심 기준 구속, 다른 구종은 구종별 차이를 둔다</summary>
        public int Velocity { get; set; } = ScoutScale.Average;

        /// <summary>제구: 목표 지점 대비 실제 위치 오차</summary>
        public int Control { get; set; } = ScoutScale.Average;

        /// <summary>스태미나: 피로 없이 던질 수 있는 투구 수</summary>
        public int Stamina { get; set; } = ScoutScale.Average;

        /// <summary>퀵모션: 투구 동작 시간 (도루 억제)</summary>
        public int HoldRunners { get; set; } = ScoutScale.Average;

        /// <summary>구종 레퍼토리</summary>
        public List<PitchRating> Repertoire { get; set; } = new List<PitchRating>();

        /// <summary>레퍼토리에서 구종 검색. 없으면 null</summary>
        public PitchRating Find(PitchType type)
        {
            for (int i = 0; i < Repertoire.Count; i++)
            {
                if (Repertoire[i].Type == type)
                {
                    return Repertoire[i];
                }
            }

            return null;
        }
    }
}
