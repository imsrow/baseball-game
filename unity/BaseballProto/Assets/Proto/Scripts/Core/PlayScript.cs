using System.Collections.Generic;
using BaseballSim.Engine.Players;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 인플레이 연출 대본. 엔진이 정한 결과(처리 야수, 시각, 주자 이동)를 시간에 따른 위치로 풀어 놓은 것.
    /// 시간은 타구 순간 기준 (s, 엔진과 같은 시간축).
    /// </summary>
    public sealed class PlayScript
    {
        public MotionTrack Ball { get; set; }

        /// <summary>공이 사라지는 시각 (홈런은 담장 너머 떨어진 뒤)</summary>
        public float BallHideTime { get; set; } = float.PositiveInfinity;

        public Dictionary<Position, MotionTrack> Fielders { get; } = new Dictionary<Position, MotionTrack>();

        public List<RunnerScript> Runners { get; } = new List<RunnerScript>();

        public List<ThrowScript> Throws { get; } = new List<ThrowScript>();

        /// <summary>공을 처리하는 야수 (홈런은 null)</summary>
        public Position? Primary { get; set; }

        /// <summary>처리 야수가 공을 잡는 시각</summary>
        public float FieldedTime { get; set; }

        public BallCamProfile CameraProfile { get; set; }

        public float End { get; set; }

        /// <summary>카메라가 화면에 담을 지점: 공 + (잡기 전) 처리 야수 또는 (송구 중) 받는 곳</summary>
        public void Focus(float t, List<Vector3> points)
        {
            points.Clear();
            Vector3 ball = Ball.Evaluate(t);
            points.Add(ball);
            // 공 바로 아래 땅: 높이 뜬 공에도 경기장이 화면에 남게
            points.Add(new Vector3(ball.x, 0f, ball.z));
            foreach (ThrowScript leg in Throws)
            {
                if (t >= leg.Start - 0.2f && t <= leg.Arrive + 0.3f)
                {
                    points.Add(leg.Target);
                    return;
                }
            }

            if (Primary.HasValue && Fielders.TryGetValue(Primary.Value, out MotionTrack fielder))
            {
                points.Add(fielder.Evaluate(t));
            }
        }
    }
}
