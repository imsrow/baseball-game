using System;
using BaseballSim.Engine.AI.ManagerAI;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 멈춤 조건 모음. 조건 객체는 처음 평가될 때의 상황을 기준점으로 기억하므로 진행 요청마다 새로 만든다.
    /// </summary>
    public static class StopConditions
    {
        /// <summary>경기 끝까지 (사람 입력이 필요하면 그 전에 멈춘다)</summary>
        public static IStopCondition EndOfGame => new NeverStop();

        /// <summary>현재 타석이 끝나 다음 타석이 시작될 때</summary>
        public static IStopCondition EndOfPlateAppearance() => new ChangeCondition(s => s.PlateAppearanceNumber);

        /// <summary>현재 반이닝이 끝날 때</summary>
        public static IStopCondition EndOfHalfInning() => new ChangeCondition(s => s.Inning * 2 + (s.IsTopHalf ? 0 : 1));

        /// <summary>현재 이닝(초·말)이 모두 끝날 때</summary>
        public static IStopCondition EndOfInning() => new ChangeCondition(s => s.Inning);

        /// <summary>지정 이닝(초 또는 말) 시작 시점</summary>
        public static IStopCondition InningReached(int inning, bool topHalf = true)
        {
            return new PlateAppearanceStartCondition(e =>
                e.State.Inning > inning || (e.State.Inning == inning && (topHalf || !e.State.IsTopHalf)));
        }

        /// <summary>내 팀이 수비 중 득점권에 주자가 나간 위기 (점수 차 조건은 InteractionConfig)</summary>
        public static IStopCondition ScoringThreat(TeamSide defendingSide)
        {
            return new PlateAppearanceStartCondition(e =>
                e.State.DefenseSide == defendingSide && e.State.HasRunnerInScoringPosition && IsClose(e));
        }

        /// <summary>내 팀이 공격 중 득점권에 주자가 나간 찬스</summary>
        public static IStopCondition ScoringChance(TeamSide battingSide)
        {
            return new PlateAppearanceStartCondition(e =>
                e.State.OffenseSide == battingSide && e.State.HasRunnerInScoringPosition && IsClose(e));
        }

        /// <summary>내 팀 공격 차례 (반이닝 시작)</summary>
        public static IStopCondition TeamBatting(TeamSide side)
        {
            return new PlateAppearanceStartCondition(e =>
                e.State.OffenseSide == side && e.State.PlateAppearancesThisHalf == 0);
        }

        /// <summary>특정 타자 차례</summary>
        public static IStopCondition PlayerUp(int playerId)
        {
            return new PlateAppearanceStartCondition(e => e.State.CurrentBatterId == playerId);
        }

        /// <summary>내 팀 투수 교체를 고려할 시점 (감독 AI가 교체를 판단하는 순간)</summary>
        public static IStopCondition PitchingChangeMoment(TeamSide defendingSide)
        {
            return new PlateAppearanceStartCondition(e =>
            {
                if (e.State.DefenseSide != defendingSide)
                {
                    return false;
                }

                var context = new ManagerContext
                {
                    State = e.State,
                    Side = defendingSide,
                    Players = e.Players,
                    Config = e.Config,
                    Random = e.State.Random,
                };
                return PitchingChangeAdvisor.Advise(context) >= 0;
            });
        }

        /// <summary>여러 조건 중 하나라도 만족하면</summary>
        public static IStopCondition Any(params IStopCondition[] conditions) => new AnyCondition(conditions);

        private static bool IsClose(GameEngine engine)
        {
            return Math.Abs(engine.State.AwayScore - engine.State.HomeScore) <= engine.Config.Interaction.ClutchMaxRunDifference;
        }

        private sealed class NeverStop : IStopCondition
        {
            public bool ShouldStop(GameEngine engine)
            {
                return false;
            }
        }
    }
}
