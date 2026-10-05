using BaseballSim.Engine.Players;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 연출 대본을 만드는 동안의 송구 계획. 주자 속도로 결과를 못 맞추면 송구 시작·속도를 범위 안에서 바꾼다.
    /// </summary>
    internal sealed class ThrowLeg
    {
        public Vector3 From;
        public int ToBase;
        public Vector3 Target;
        public Position Receiver;

        public float Start;
        public float Duration;

        /// <summary>송구를 가장 빨리 시작할 수 있는 시각 (공을 잡은 뒤 최소 준비 시간)</summary>
        public float EarliestStart;

        public float MinDuration;
        public float MaxDuration;

        /// <summary>이 송구로 아웃·세이프가 갈리는 주자 (없으면 null)</summary>
        public RunnerPlan Runner;

        public bool RunnerOut;

        /// <summary>악송구: 베이스를 지나쳐 간다</summary>
        public bool Overshoot;

        public float Arrive => Start + Duration;
    }
}
