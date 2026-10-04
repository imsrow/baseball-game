using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 투수 결정: 구종과 목표 지점. 실제 위치·구속은 엔진이 정한다.
    /// </summary>
    public sealed class PitchCall
    {
        public PitchCall()
        {
        }

        public PitchCall(PitchType type, PlateLocation target, double? releaseQuality = null)
        {
            Type = type;
            Target = target;
            ReleaseQuality = releaseQuality;
        }

        public PitchType Type { get; set; }

        public PlateLocation Target { get; set; }

        /// <summary>사람 조작 릴리스 품질 (−1 ~ +1). AI는 null(중립)</summary>
        public double? ReleaseQuality { get; set; }
    }
}
