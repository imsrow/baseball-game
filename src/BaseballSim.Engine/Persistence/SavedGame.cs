using System.Collections.Generic;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 불러온 저장 데이터
    /// </summary>
    public sealed class SavedGame
    {
        public GameState State { get; set; }

        /// <summary>양 팀 로스터·능력치 스냅샷</summary>
        public PlayerDirectory Players { get; set; }

        /// <summary>팀 ID → 팀 이름</summary>
        public Dictionary<int, string> TeamNames { get; set; } = new Dictionary<int, string>();

        public ControlModes Modes { get; set; }

        /// <summary>저장 당시 설정 지문과 지금 설정이 같은지 (다르면 이어서 진행한 결과가 달라질 수 있다)</summary>
        public bool ConfigMatches { get; set; }
    }
}
