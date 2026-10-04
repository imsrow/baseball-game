using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 순수 시뮬레이션: 양 팀 모든 역할을 AI로 두고 경기를 끝까지 진행한다.
    /// 직접 플레이와 같은 상태 머신·판정 코드를 사용한다.
    /// </summary>
    public static class GameSimulator
    {
        public static GameState Play(GameSetup setup, LeagueConfig config, ulong seed, IEventSink sink = null)
        {
            var engine = new GameEngine(setup, config, seed, AiControllers.CreateAllAi(), sink);
            engine.RunUntil(StopConditions.EndOfGame);
            return engine.State;
        }
    }
}
