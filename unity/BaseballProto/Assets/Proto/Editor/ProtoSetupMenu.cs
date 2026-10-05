using System.Diagnostics;
using System.IO;
using BaseballProto.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BaseballProto.EditorTools
{
    /// <summary>
    /// 에디터 메뉴: 프로토 씬 생성·빌드 설정, 엔진 DLL 동기화
    /// </summary>
    public static class ProtoSetupMenu
    {
        private const string ScenePath = "Assets/Proto/Scenes/Duel.unity";

        [MenuItem("Baseball/Setup Proto Scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ProtoAssets.EnsureMaterial();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("ProtoBootstrap").AddComponent<ProtoBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            ProtoBuild.ApplyOrientationSettings();

            // WebGL: 홈 화면 추가용 PWA 템플릿, 서버 설정 없이도 열리도록 압축 해제 대체 경로
            PlayerSettings.WebGL.template = "APPLICATION:PWA";
            PlayerSettings.WebGL.decompressionFallback = true;

            PlayerSettings.productName = "BaseballProto";
            AssetDatabase.SaveAssets();
            Debug.Log("프로토 씬 생성 완료: " + ScenePath + " (빌드 목록 등록, 자동 회전, WebGL PWA 템플릿)");
        }

        [MenuItem("Baseball/Sync Engine DLL")]
        public static void SyncEngine()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string script = Path.Combine(projectRoot, "tools", "sync-engine.ps1");
            var info = new ProcessStartInfo("powershell", "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = projectRoot,
            };

            using (Process process = Process.Start(info))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Debug.Log(output);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogError("엔진 동기화 실패\n" + output + "\n" + error);
                }
            }
        }
    }
}
