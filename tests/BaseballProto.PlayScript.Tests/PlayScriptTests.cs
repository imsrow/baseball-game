using System.Collections.Generic;
using System.Linq;
using BaseballProto.Core;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.Teams;
using UnityEngine;
using Xunit;

namespace BaseballProto.PlayScript.Tests
{
    /// <summary>
    /// 인플레이 연출 대본이 엔진 결과와 맞는지: AI 경기의 실제 인플레이 이벤트를 대본으로 만들어 검사한다.
    /// </summary>
    public class PlayScriptTests
    {
        private const int Games = 80;

        // 주자가 다음 베이스 쪽으로 이만큼은 움직여야 "뛰었다"고 본다
        private const float MinRunM = 3f;

        private static readonly LeagueConfig Config = new LeagueConfig();
        private static readonly FieldGeometry Field = new FieldGeometry(Config.Field);
        private static readonly List<(PitchEvent Event, PlayerDirectory Players)> Plays = Collect();

        private static List<(PitchEvent, PlayerDirectory)> Collect()
        {
            var plays = new List<(PitchEvent, PlayerDirectory)>();
            for (int g = 0; g < Games; g++)
            {
                var engine = new GameEngine(MatchFactory.Create(g + 1, 700 + g), Config, (ulong)(700 + g), AiControllers.CreateAllAi());
                engine.RunUntil(StopConditions.EndOfGame);
                foreach (PitchEvent ev in engine.State.Log.OfType<PitchEvent>())
                {
                    if (ev.Result == PitchResult.InPlay && ev.BattedBall != null)
                    {
                        plays.Add((ev, engine.Players));
                    }
                }
            }

            return plays;
        }

        private static BaseballProto.Core.PlayScript Build((PitchEvent Event, PlayerDirectory Players) play)
        {
            var builder = new PlayScriptBuilder(Config, Field, new ProtoTuning());
            return builder.Build(play.Event, new Vector3(0f, 0.8f, 0f), -1f, play.Players);
        }

        private static Vector3 BasePoint(int b)
        {
            FieldPoint p = Field.Base(b);
            return new Vector3((float)p.X, 0f, (float)p.Y);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        /// <summary>타구 순간 주자 상황에서 포스 상태인 루 (1루는 늘, 2루는 1루가 찼을 때, 3루는 1·2루가 찼을 때)</summary>
        private static List<(int Base, int PlayerId)> ForcedRunners(PitchEvent ev)
        {
            var forced = new List<(int, int)>();
            if (ev.RunnerOnFirst >= 0)
            {
                forced.Add((1, ev.RunnerOnFirst));
                if (ev.RunnerOnSecond >= 0)
                {
                    forced.Add((2, ev.RunnerOnSecond));
                    if (ev.RunnerOnThird >= 0)
                    {
                        forced.Add((3, ev.RunnerOnThird));
                    }
                }
            }

            return forced;
        }

        /// <summary>땅볼 (뜬 번트가 잡힌 경우는 주자가 귀루하므로 제외)</summary>
        private static bool IsGround(PitchEvent ev)
        {
            if (ev.PlateAppearanceOutcome == PlateAppearanceOutcome.PopOut || ev.PlateAppearanceOutcome == PlateAppearanceOutcome.LineOut
                || ev.PlateAppearanceOutcome == PlateAppearanceOutcome.FlyOut)
            {
                return false;
            }

            return ev.IsBunt || ev.BattedBall.LaunchAngleDeg < Config.Fielding.GroundBallMaxLaunchAngleDeg;
        }

        [Fact]
        public void 땅볼_포스주자는_아웃카운트와_관계없이_다음베이스로_뛴다()
        {
            int checkedRunners = 0;
            int twoOutChecked = 0;
            foreach (var play in Plays.Where(p => IsGround(p.Event)))
            {
                BaseballProto.Core.PlayScript script = Build(play);
                foreach ((int b, int id) in ForcedRunners(play.Event))
                {
                    RunnerScript runner = script.Runners.Single(r => r.PlayerId == id);
                    Vector3 start = BasePoint(b);
                    Vector3 next = BasePoint(b + 1);
                    // 다음 베이스 쪽으로 간 최대 거리
                    float best = 0f;
                    for (float t = 0f; t <= script.End; t += 0.1f)
                    {
                        Vector3 p = runner.Track.Evaluate(t);
                        best = Mathf.Max(best, Flat(start, next) - Flat(p, next));
                    }

                    string where = $"{play.Event.PlateAppearanceOutcome} outs {play.Event.OutsBefore} runner on {b}";
                    Assert.True(best >= MinRunM, where + $": 포스 주자가 {best:0.0} m만 움직임");
                    checkedRunners++;
                    if (play.Event.OutsBefore == Config.Rules.OutsPerHalfInning - 1)
                    {
                        twoOutChecked++;
                    }
                }
            }

            Assert.True(checkedRunners > 100, "검사한 포스 주자 " + checkedRunners);
            Assert.True(twoOutChecked > 20, "2아웃 포스 주자 " + twoOutChecked);
        }

        [Fact]
        public void 아웃이면_공이_먼저_세이프면_주자가_먼저_도착한다()
        {
            int outs = 0;
            foreach (var play in Plays)
            {
                BaseballProto.Core.PlayScript script = Build(play);
                foreach (RunnerScript runner in script.Runners)
                {
                    Vector3 end = runner.Track.EndPosition;
                    if (Flat(end, runner.Track.Start) < 0.5f || float.IsInfinity(runner.OutTime))
                    {
                        continue;
                    }

                    ThrowScript leg = script.Throws.LastOrDefault(th => Flat(th.Target, end) < 0.6f);
                    if (leg == null)
                    {
                        continue;
                    }

                    Assert.True(runner.Track.EndTime >= leg.Arrive + 0.05f,
                        $"{play.Event.PlateAppearanceOutcome}: 아웃인데 주자 {runner.Track.EndTime:0.00} s, 공 {leg.Arrive:0.00} s");
                    outs++;
                }
            }

            Assert.True(outs > 300, "검사한 송구 아웃 " + outs);
        }

        [Fact]
        public void 공은_엔진이_정한_낙하_포구_지점을_그_시각에_지난다()
        {
            int checkedBalls = 0;
            foreach (var play in Plays)
            {
                PitchEvent ev = play.Event;
                BattedBallData b = ev.BattedBall;
                // 번트는 별도 모델이라 비행 시각이 없다
                if (ev.PlateAppearanceOutcome == PlateAppearanceOutcome.HomeRun || ev.IsBunt || IsGround(ev))
                {
                    continue;
                }

                BaseballProto.Core.PlayScript script = Build(play);
                Vector3 expected;
                float at;
                if (b.HasLanding)
                {
                    expected = new Vector3((float)b.LandingX, 0f, (float)b.LandingY);
                    at = (float)b.LandingTimeS;
                }
                else
                {
                    expected = new Vector3((float)b.EndX, (float)Config.Physics.CatchHeightM, (float)b.EndY);
                    at = (float)b.FieldedTimeS;
                }

                Assert.True((script.Ball.Evaluate(at) - expected).magnitude < 0.1f, $"{ev.PlateAppearanceOutcome} 공 위치 어긋남");
                checkedBalls++;
            }

            Assert.True(checkedBalls > 300);
        }

        [Fact]
        public void 병살은_2루_송구_뒤_1루_송구()
        {
            int doublePlays = 0;
            foreach (var play in Plays.Where(p => p.Event.PlateAppearanceOutcome == PlateAppearanceOutcome.GroundedIntoDoublePlay))
            {
                BaseballProto.Core.PlayScript script = Build(play);
                Assert.Equal(2, script.Throws.Count);
                Assert.True(Flat(script.Throws[0].Target, BasePoint(2)) < 0.6f);
                Assert.True(Flat(script.Throws[1].Target, BasePoint(1)) < 0.6f);
                Assert.True(script.Throws[1].Start >= script.Throws[0].Arrive);
                doublePlays++;
            }

            Assert.True(doublePlays > 5, "병살 " + doublePlays);
        }

        [Fact]
        public void 처리_야수는_처리_시각에_그_자리에_있다()
        {
            foreach (var play in Plays)
            {
                BattedBallData b = play.Event.BattedBall;
                if (play.Event.PlateAppearanceOutcome == PlateAppearanceOutcome.HomeRun || !b.FieldedBy.HasValue
                    || b.FieldedTimeS <= 0)
                {
                    continue;
                }

                BaseballProto.Core.PlayScript script = Build(play);
                Vector3 fielder = script.Fielders[b.FieldedBy.Value].Evaluate((float)b.FieldedTimeS);
                Assert.True(Flat(fielder, new Vector3((float)b.EndX, 0f, (float)b.EndY)) < 0.6f,
                    $"{play.Event.PlateAppearanceOutcome} {b.FieldedBy} 처리 지점에 없음");
            }
        }
    }
}
