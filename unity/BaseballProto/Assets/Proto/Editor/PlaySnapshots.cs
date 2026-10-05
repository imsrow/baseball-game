using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseballProto.Core;
using BaseballProto.View;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Simulation;
using UnityEditor;
using UnityEngine;

namespace BaseballProto.EditorTools
{
    /// <summary>
    /// 인플레이 연출 확인용 스냅샷 (에디터 전용, 빌드에 들어가지 않음).
    /// AI 경기에서 타구 종류별 실제 이벤트를 골라 연출을 재생하며 카메라 화면을 일정 간격으로 찍어 한 장(PNG)에 모은다. 세로·가로 각각.
    /// 배치 모드: Unity.exe -batchmode -quit -projectPath ... -executeMethod BaseballProto.EditorTools.PlaySnapshots.RenderBatch
    ///   출력 폴더는 환경 변수 PLAY_SNAPSHOT_DIR (없으면 프로젝트 Builds/Snapshots)
    /// </summary>
    public static class PlaySnapshots
    {
        private const int FramesPerPlay = 6;
        private const float StepS = 1f / 60f;
        private const int ThumbLong = 360;
        private const int ThumbShort = 202;

        private static readonly PlateAppearanceOutcome[] Wanted =
        {
            PlateAppearanceOutcome.HomeRun, PlateAppearanceOutcome.Double, PlateAppearanceOutcome.FlyOut,
            PlateAppearanceOutcome.GroundedIntoDoublePlay, PlateAppearanceOutcome.GroundOut, PlateAppearanceOutcome.Single,
        };

        [MenuItem("Baseball/Render Play Snapshots")]
        public static void RenderMenu()
        {
            Render();
        }

        public static void RenderBatch()
        {
            bool ok;
            try
            {
                ok = Render();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ok = false;
            }

            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Render()
        {
            string dir = Environment.GetEnvironmentVariable("PLAY_SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir))
            {
                dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Snapshots");
            }

            Directory.CreateDirectory(dir);
            var config = new LeagueConfig();
            List<(PitchEvent ev, GameEngine engine)> plays = FindPlays(config);
            Debug.Log("PlaySnapshots: plays " + string.Join(", ", plays.Select(p => p.ev.PlateAppearanceOutcome.ToString())));

            RenderSheet(config, plays, 720, 1280, Path.Combine(dir, "plays_portrait.png"));
            RenderSheet(config, plays, 1280, 720, Path.Combine(dir, "plays_landscape.png"));
            return true;
        }

        private static List<(PitchEvent, GameEngine)> FindPlays(LeagueConfig config)
        {
            var found = new Dictionary<PlateAppearanceOutcome, (PitchEvent, GameEngine)>();
            for (int g = 0; g < 60 && found.Count < Wanted.Length; g++)
            {
                var engine = new GameEngine(MatchFactory.Create(g + 1, 500 + g), config, (ulong)(500 + g), AiControllers.CreateAllAi());
                engine.RunUntil(StopConditions.EndOfGame);
                foreach (PitchEvent ev in engine.State.Log.OfType<PitchEvent>())
                {
                    if (ev.Result != PitchResult.InPlay || ev.BattedBall == null || !ev.PlateAppearanceOutcome.HasValue || ev.IsBunt)
                    {
                        continue;
                    }

                    PlateAppearanceOutcome o = ev.PlateAppearanceOutcome.Value;
                    bool runners = ev.RunnerOnFirst >= 0 || ev.RunnerOnSecond >= 0;
                    // 주자가 있는 장면을 우선 (진루·송구까지 보이게)
                    if (Wanted.Contains(o) && !found.ContainsKey(o) && (runners || o == PlateAppearanceOutcome.HomeRun
                        || o == PlateAppearanceOutcome.FlyOut))
                    {
                        found[o] = (ev, engine);
                    }
                }
            }

            return Wanted.Where(found.ContainsKey).Select(o => found[o]).ToList();
        }

        private static void RenderSheet(LeagueConfig config, List<(PitchEvent ev, GameEngine engine)> plays, int width, int height,
            string path)
        {
            bool portrait = height > width;
            int thumbW = portrait ? ThumbShort : ThumbLong;
            int thumbH = portrait ? ThumbLong : ThumbShort;
            var sheet = new Texture2D(thumbW * FramesPerPlay, thumbH * plays.Count, TextureFormat.RGB24, false);
            var rt = new RenderTexture(width, height, 24);
            var small = new RenderTexture(thumbW, thumbH, 0);
            var read = new Texture2D(thumbW, thumbH, TextureFormat.RGB24, false);

            var tuning = new ProtoTuning();
            var root = new GameObject("SnapshotField").transform;
            var cameraGo = new GameObject("SnapshotCamera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ProtoColors.Sky;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            camera.targetTexture = rt;
            camera.aspect = (float)width / height;
            var lightGo = new GameObject("SnapshotSun");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            try
            {
                var field = new FieldView(root, camera, config.StrikeZone, tuning);
                var shake = new CameraShake(camera.transform);
                var ball = new BallView(root, tuning.BallVisualDiameterM, tuning.BallShadowDiameterM);
                var geometry = new FieldGeometry(config.Field);
                new StadiumView(root, geometry);
                var fielders = new FieldersView(root, geometry);
                var runners = new RunnersView(root, geometry);
                var director = new PlayDirector(config, geometry, tuning, field, ball, fielders, runners,
                    new BallCamera(camera, field, shake, tuning));

                for (int row = 0; row < plays.Count; row++)
                {
                    (PitchEvent ev, GameEngine engine) = plays[row];
                    director.ResetForPitch(engine.State);
                    runners.HideAll();
                    field.ApplyCamera();
                    var contact = new Vector3((float)ev.PlateX, (float)ev.PlateZ, 0f);
                    PlayScript script = director.Build(ev, contact, -1f, engine.Players);
                    director.Begin(script);

                    // 재생 시간(화면 시간)을 프레임 수로 미리 세어 고르게 찍는다
                    float display = script.End / Mathf.Max(tuning.PlaySpeed,
                        tuning.PlaySpeed * Mathf.Max(1f, script.End / tuning.PlaySpeed / tuning.MaxPlayDisplayS));
                    int totalSteps = Mathf.CeilToInt(display / StepS);
                    int shot = 0;
                    for (int step = 0; step <= totalSteps + 30 && shot < FramesPerPlay; step++)
                    {
                        int target = Mathf.RoundToInt((float)shot / (FramesPerPlay - 1) * totalSteps);
                        if (step >= target)
                        {
                            Capture(camera, rt, small, read, sheet, shot * thumbW, (plays.Count - 1 - row) * thumbH);
                            shot++;
                        }

                        director.Tick(StepS);
                    }
                }

                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Debug.Log("PlaySnapshots: wrote " + path);
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(root.gameObject);
                UnityEngine.Object.DestroyImmediate(cameraGo);
                UnityEngine.Object.DestroyImmediate(lightGo);
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(small);
            }
        }

        private static void Capture(Camera camera, RenderTexture rt, RenderTexture small, Texture2D read, Texture2D sheet, int x, int y)
        {
            camera.Render();
            Graphics.Blit(rt, small);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = small;
            read.ReadPixels(new Rect(0, 0, small.width, small.height), 0, 0);
            read.Apply();
            RenderTexture.active = previous;
            sheet.SetPixels(x, y, small.width, small.height, read.GetPixels());
        }
    }
}
