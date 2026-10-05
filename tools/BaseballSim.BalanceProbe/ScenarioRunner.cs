using System;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 시나리오 하나를 여러 경기 돌린다. 사람 역할은 양 팀에 배정하고 Unity와 같은 Submit 경로로 입력한다.
    /// 경기 시드·사람 입력 난수는 고정이라 같은 설정이면 같은 결과가 나온다.
    /// </summary>
    public static class ScenarioRunner
    {
        private const ulong SeedBase = 1000;
        private const int InputSeed = 12345;

        // (e) 시나리오: TUNE의 Release sigma 최대값
        private const double MaxReleaseSigmaCap = 1.5;

        private sealed class NeverStop : IStopCondition
        {
            public bool ShouldStop(GameEngine engine) => false;
        }

        public static ProbeStats Run(Scenario scenario, int games, Func<LeagueConfig> createConfig)
        {
            LeagueConfig config = createConfig();
            if (scenario == Scenario.EdgePerfectMaxCap)
            {
                config.InputModifier.MaxPitchExecutionSigmaLogShift = MaxReleaseSigmaCap;
            }

            var random = new Random(InputSeed);
            var pitcher = new HumanLikePitcher(random);
            var batter = new HumanLikeBatter(random);
            var stats = new ProbeStats();
            var never = new NeverStop();

            for (int g = 0; g < games; g++)
            {
                ControllerSet controllers = AiControllers.CreateAllAi();
                foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
                {
                    if (ScenarioInfo.IsHumanBatting(scenario))
                    {
                        controllers.Assign(side, HumanBattingDecision.Instance);
                    }
                    else if (ScenarioInfo.IsHumanPitching(scenario))
                    {
                        controllers.Assign(side, HumanPitchingDecision.Instance);
                    }
                }

                var engine = new GameEngine(ProbeTeams.Create(g), config, SeedBase + (ulong)g, controllers);
                while (engine.RunUntil(never).Status != RunStatus.GameOver)
                {
                    PendingDecision pending = engine.Pending;
                    SubmitResult result = pending.Kind == DecisionKind.Swing
                        ? engine.Submit(batter.Choose(scenario, pending.BattingContext))
                        : engine.Submit(pitcher.Choose(scenario, pending.PitchingContext));
                    if (!result.Accepted)
                    {
                        throw new InvalidOperationException("입력 거절: " + result.Reason);
                    }
                }

                stats.Add(engine.State.Log);
            }

            return stats;
        }
    }
}
