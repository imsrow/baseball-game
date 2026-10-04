using System.Collections.Generic;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 투구 한 개의 완전한 기록: 상황(카운트·주자·아웃), 투구(구종·구속·위치), 결과, 타구, 주자 이동
    /// </summary>
    public sealed class PitchEvent : GameEvent
    {
        // ── 상황 (투구 전) ──
        public int PlateAppearanceNumber { get; set; }
        public int PitchNumberInPlateAppearance { get; set; }
        public int BatterId { get; set; }
        public int PitcherId { get; set; }
        public int CatcherId { get; set; }
        public Hand BattingHand { get; set; }
        public Hand PitcherHand { get; set; }
        public int BallsBefore { get; set; }
        public int StrikesBefore { get; set; }
        public int OutsBefore { get; set; }

        /// <summary>투구 전 1·2·3루 주자 ID (없으면 −1)</summary>
        public int RunnerOnFirst { get; set; } = -1;
        public int RunnerOnSecond { get; set; } = -1;
        public int RunnerOnThird { get; set; } = -1;

        public int AwayScoreBefore { get; set; }
        public int HomeScoreBefore { get; set; }

        // ── 투구 ──
        public PitchType PitchType { get; set; }
        public double VelocityKmh { get; set; }
        public double TargetX { get; set; }
        public double TargetZ { get; set; }
        public double PlateX { get; set; }
        public double PlateZ { get; set; }
        public AttackRegion Region { get; set; }
        public bool IsInZone { get; set; }
        public double PerceivedX { get; set; }
        public double PerceivedZ { get; set; }

        /// <summary>사람이 결정한 투구인지 (AI면 false)</summary>
        public bool PitchByHuman { get; set; }

        /// <summary>사람이 결정한 스윙 판단인지</summary>
        public bool SwingByHuman { get; set; }

        // ── 결과 ──
        public bool Swung { get; set; }
        public PitchResult Result { get; set; }
        public BattedBallData BattedBall { get; set; }

        /// <summary>타석이 이 투구로 끝났으면 결과, 아니면 null</summary>
        public PlateAppearanceOutcome? PlateAppearanceOutcome { get; set; }

        public List<RunnerMovement> RunnerMovements { get; set; } = new List<RunnerMovement>();
        public bool IsError { get; set; }

        /// <summary>포스아웃으로 이닝이 끝나 득점이 무효 처리됨</summary>
        public bool RunsNullified { get; set; }

        public int RunsScored { get; set; }
        public int RunsBattedIn { get; set; }
        public int OutsAfter { get; set; }
        public int AwayScoreAfter { get; set; }
        public int HomeScoreAfter { get; set; }
    }
}
