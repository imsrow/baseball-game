using System.Collections.Generic;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.State
{
    /// <summary>
    /// 경기 중 한 팀의 상태: 타순, 다음 타자, 현재 투수, 등판 이력, 가용 불펜·벤치, 교체되어 나간 선수
    /// </summary>
    public sealed class TeamGameState
    {
        public int TeamId { get; set; }

        public List<LineupSlot> Lineup { get; set; } = new List<LineupSlot>();

        /// <summary>다음 타자 타순 인덱스 (0~8)</summary>
        public int NextBatterIndex { get; set; }

        public int CurrentPitcherId { get; set; }

        /// <summary>등판한 투수 (등판 순서)</summary>
        public List<PitcherGameState> Pitchers { get; set; } = new List<PitcherGameState>();

        /// <summary>아직 등판하지 않은 불펜 투수 (앞쪽 우선)</summary>
        public List<int> AvailableBullpen { get; set; } = new List<int>();

        /// <summary>마무리 투수 (−1 없음)</summary>
        public int CloserId { get; set; } = -1;

        /// <summary>셋업 투수</summary>
        public List<int> SetupIds { get; set; } = new List<int>();

        /// <summary>아직 출전하지 않은 벤치 야수</summary>
        public List<int> Bench { get; set; } = new List<int>();

        /// <summary>교체되어 재출전할 수 없는 선수</summary>
        public List<int> Removed { get; set; } = new List<int>();

        /// <summary>주루 성향 (추가 진루·태그업 판단). 사람 감독이 바꾼다, AI는 Normal</summary>
        public Control.BaserunningStyle BaserunningStyle { get; set; }

        public int Runs { get; set; }

        public int Hits { get; set; }

        public int Errors { get; set; }

        public PitcherGameState CurrentPitcher
        {
            get
            {
                for (int i = Pitchers.Count - 1; i >= 0; i--)
                {
                    if (Pitchers[i].PlayerId == CurrentPitcherId)
                    {
                        return Pitchers[i];
                    }
                }

                return null;
            }
        }

        public PitcherGameState FindPitcher(int playerId)
        {
            for (int i = 0; i < Pitchers.Count; i++)
            {
                if (Pitchers[i].PlayerId == playerId)
                {
                    return Pitchers[i];
                }
            }

            return null;
        }

        /// <summary>라인업에서 선수의 타순 인덱스 (없으면 −1)</summary>
        public int LineupIndexOf(int playerId)
        {
            for (int i = 0; i < Lineup.Count; i++)
            {
                if (Lineup[i].PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>해당 수비 포지션 선수 ID (투수는 현재 투수). 없으면 −1</summary>
        public int PlayerIdAt(Position position)
        {
            if (position == Position.Pitcher)
            {
                return CurrentPitcherId;
            }

            for (int i = 0; i < Lineup.Count; i++)
            {
                if (Lineup[i].Position == position)
                {
                    return Lineup[i].PlayerId;
                }
            }

            return -1;
        }
    }
}
