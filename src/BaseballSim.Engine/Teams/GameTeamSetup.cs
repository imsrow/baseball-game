using System.Collections.Generic;

namespace BaseballSim.Engine.Teams
{
    /// <summary>
    /// 한 경기 출전 구성: 타순(9명, 지명타자 포함), 선발투수, 불펜 순서, 벤치
    /// </summary>
    public sealed class GameTeamSetup
    {
        public Team Team { get; set; }

        /// <summary>타순 1~9번 (지명타자 제도에서 투수는 포함하지 않는다)</summary>
        public List<LineupSlot> Lineup { get; set; } = new List<LineupSlot>();

        public int StartingPitcherId { get; set; }

        /// <summary>불펜 투수 (앞쪽일수록 우선 기용)</summary>
        public List<int> Bullpen { get; set; } = new List<int>();

        /// <summary>벤치 야수</summary>
        public List<int> Bench { get; set; } = new List<int>();
    }
}
