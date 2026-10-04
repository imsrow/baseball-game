using System.Collections.Generic;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Simulation;

namespace BaseballSim.Engine.State
{
    /// <summary>
    /// 경기 상태 전체. 경기를 이어가는 데 필요한 모든 정보(난수 상태, 이벤트 로그 포함)가 여기에 있다.
    /// 컨트롤러(사람/AI)는 상태에 포함되지 않으므로 언제든 교체해도 경기가 끊기지 않는다.
    /// </summary>
    public sealed class GameState
    {
        public int GameId { get; set; }

        public GamePhase Phase { get; set; } = GamePhase.OffenseManager;

        public int Inning { get; set; } = 1;

        public bool IsTopHalf { get; set; } = true;

        public int Outs { get; set; }

        public int Balls { get; set; }

        public int Strikes { get; set; }

        /// <summary>인덱스 0 = 1루, 1 = 2루, 2 = 3루. 비어 있으면 null</summary>
        public BaseRunner[] Bases { get; set; } = new BaseRunner[3];

        public TeamGameState Away { get; set; }

        public TeamGameState Home { get; set; }

        /// <summary>경기 내 타석 번호 (1부터)</summary>
        public int PlateAppearanceNumber { get; set; } = 1;

        /// <summary>현재 타석 투구 번호 (다음 투구가 몇 번째인지, 1부터)</summary>
        public int PitchNumberInPlateAppearance { get; set; } = 1;

        public int EventSequence { get; set; }

        /// <summary>이번 반이닝에 끝난 타석 수 (이닝 시작 판단용)</summary>
        public int PlateAppearancesThisHalf { get; set; }

        /// <summary>이번 투구에 도루를 시도하는 주자의 출발 베이스 (0이면 없음)</summary>
        public int StealFromBase { get; set; }

        /// <summary>이번 투구 번트 사인</summary>
        public Control.BuntType BuntSign { get; set; }

        /// <summary>Swing 단계에서 타자가 판단할 투구</summary>
        public PitchInFlight CurrentPitch { get; set; }

        public List<GameEvent> Log { get; set; } = new List<GameEvent>();

        public Pcg32Random Random { get; set; }

        public bool IsGameOver => Phase == GamePhase.GameOver;

        public TeamSide OffenseSide => IsTopHalf ? TeamSide.Away : TeamSide.Home;

        public TeamSide DefenseSide => IsTopHalf ? TeamSide.Home : TeamSide.Away;

        public TeamGameState Offense => IsTopHalf ? Away : Home;

        public TeamGameState Defense => IsTopHalf ? Home : Away;

        public int AwayScore => Away.Runs;

        public int HomeScore => Home.Runs;

        public TeamGameState Team(TeamSide side)
        {
            return side == TeamSide.Away ? Away : Home;
        }

        public int CurrentBatterId => Offense.Lineup[Offense.NextBatterIndex].PlayerId;

        /// <summary>승리 팀 (경기 종료 전이거나 무승부면 null)</summary>
        public TeamSide? Winner
        {
            get
            {
                if (!IsGameOver || AwayScore == HomeScore)
                {
                    return null;
                }

                return AwayScore > HomeScore ? TeamSide.Away : TeamSide.Home;
            }
        }

        public bool HasRunnerInScoringPosition => Bases[1] != null || Bases[2] != null;
    }
}
