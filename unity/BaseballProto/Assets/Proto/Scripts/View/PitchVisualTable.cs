using BaseballSim.Engine.Pitching;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 구종별 휘는 연출 크기 (m). x: 투수 팔 쪽 +, y: 위 +. 연출 전용이며 실제 도착 위치는 엔진이 정한다.
    /// </summary>
    public static class PitchVisualTable
    {
        public static Vector2 Break(PitchType type)
        {
            switch (type)
            {
                case PitchType.FourSeam: return new Vector2(0.05f, 0.10f);
                case PitchType.Sinker: return new Vector2(0.20f, -0.12f);
                case PitchType.Cutter: return new Vector2(-0.10f, 0f);
                case PitchType.Slider: return new Vector2(-0.25f, -0.08f);
                case PitchType.Curveball: return new Vector2(-0.15f, -0.35f);
                case PitchType.Changeup: return new Vector2(0.15f, -0.20f);
                case PitchType.Splitter: return new Vector2(0.03f, -0.30f);
                default: return Vector2.zero;
            }
        }
    }
}
