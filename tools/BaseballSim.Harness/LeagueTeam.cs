using System.Collections.Generic;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 하네스용 팀 구성: 고정 타순, 선발 로테이션, 불펜 순서, 벤치
    /// </summary>
    public sealed class LeagueTeam
    {
        public Team Team { get; set; }

        public List<LineupSlot> Lineup { get; set; } = new List<LineupSlot>();

        public List<int> Rotation { get; set; } = new List<int>();

        /// <summary>불펜 기용 순서 (앞쪽 먼저 등판, 뒤쪽일수록 좋은 투수)</summary>
        public List<int> Bullpen { get; set; } = new List<int>();

        /// <summary>마무리 (불펜 최고 투수)</summary>
        public int CloserId { get; set; } = -1;

        /// <summary>셋업 (불펜 2·3번째 투수)</summary>
        public List<int> SetupIds { get; set; } = new List<int>();

        public List<int> Bench { get; set; } = new List<int>();

        public int GamesPlayed { get; set; }

        /// <summary>구원투수별 등판 기록 (휴식 판단용)</summary>
        public Dictionary<int, ReliefRecord> ReliefLog { get; set; } = new Dictionary<int, ReliefRecord>();
    }
}
