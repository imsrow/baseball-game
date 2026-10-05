using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BaseballProto.EditorTools
{
    /// <summary>
    /// 빌드 메뉴·배치 모드 진입점.
    /// 배치 모드: Unity.exe -batchmode -quit -projectPath ... -buildTarget WebGL -executeMethod BaseballProto.EditorTools.ProtoBuild.BuildWebGLBatch
    /// </summary>
    public static class ProtoBuild
    {
        private const string WebGLOutput = "Builds/WebGL";
        private const string WebGLTemplate = "PROJECT:BaseballPWA";

        [MenuItem("Baseball/Build WebGL")]
        public static void BuildWebGLMenu()
        {
            BuildWebGL();
        }

        public static void BuildWebGLBatch()
        {
            bool ok = BuildWebGL();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>세로·가로 자동 회전 (거꾸로 세로 제외). Android 빌드에도 적용된다</summary>
        public static void ApplyOrientationSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        private static bool BuildWebGL()
        {
            ApplyOrientationSettings();
            // GitHub Pages는 Content-Encoding 헤더를 붙일 수 없다.
            // Gzip + 압축 해제 대체 경로(JS에서 해제): 전송량은 줄이고 서버 설정 없이도 열린다.
            // Brotli는 JS 해제가 느려 Gzip을 쓴다.
            ProtoAssets.EnsureMaterial();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = WebGLTemplate;

            // 파일 이름을 내용 해시로: 새 빌드를 올리면 이름이 바뀌어 브라우저·서비스 워커 캐시에 옛 파일이 남지 않는다
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("빌드할 씬이 없습니다. Baseball > Setup Proto Scene을 먼저 실행하세요.");
                return false;
            }

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", WebGLOutput));
            if (Directory.Exists(output))
            {
                Directory.Delete(output, true);
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log("[ProtoBuild] WebGL " + summary.result + " size " + (summary.totalSize / (1024 * 1024)) + " MB, "
                + summary.totalTime.TotalSeconds.ToString("0") + " s → " + output);
            return summary.result == BuildResult.Succeeded;
        }
    }
}
