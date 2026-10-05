using System.Collections.Generic;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 인플레이 판정 결과
    /// </summary>
    public sealed class PlayResult
    {
        public PlateAppearanceOutcome Outcome { get; set; }

        public BattedBallType BallType { get; set; }

        /// <summary>타구를 처리한 야수 포지션 (홈런은 null)</summary>
        public Position? FieldedBy { get; set; }

        public bool IsError { get; set; }

        public int OutsRecorded { get; set; }

        /// <summary>주자·타자주자 이동 (가만히 있는 주자도 포함)</summary>
        public List<RunnerMovement> Movements { get; set; } = new List<RunnerMovement>();

        /// <summary>뜬공 비행 결과 (땅볼은 null)</summary>
        public FlightResult Flight { get; set; }

        /// <summary>공이 처리되거나 떨어진 지점</summary>
        public FieldPoint BallEndPoint { get; set; }

        /// <summary>체공시간 (땅볼은 0)</summary>
        public double HangTimeS { get; set; }

        // ── 연출용 (판정에는 쓰지 않음) ──

        /// <summary>뜬공이 잡히지 않고 땅·펜스에 처음 닿은 지점 (잡혔거나 땅볼·홈런이면 null)</summary>
        public FieldPoint? LandingPoint { get; set; }

        /// <summary>LandingPoint 도달 시각 (타구 순간 기준, s)</summary>
        public double LandingTimeS { get; set; }

        /// <summary>처리 야수가 공을 잡은(포구·땅볼 처리·외야 회수) 시각 (s). 홈런은 0</summary>
        public double FieldedTimeS { get; set; }

        /// <summary>처리 야수가 처리 지점에 도착할 수 있는 엔진 기준 시각 (반응 + 이동, s)</summary>
        public double FielderArrivalS { get; set; }

        /// <summary>포스·타자 아웃으로 이닝이 끝나 득점이 무효가 됐는지</summary>
        public bool RunsNullified { get; set; }

        public int RunsScored
        {
            get
            {
                if (RunsNullified)
                {
                    return 0;
                }

                int runs = 0;
                foreach (RunnerMovement m in Movements)
                {
                    if (m.Scored)
                    {
                        runs++;
                    }
                }

                return runs;
            }
        }
    }
}
