using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 커서 위치 프레임 단위 기록 (드래그 떨림 검증용).
    /// CSV: 프레임, 시각, 이번 프레임 이동 이벤트 수, 이벤트 이동량 합(px), 커서(m), 커서 화면 위치(px), 직전 프레임 대비 커서 이동(px).
    /// 커서 이동은 화면 흔들림 영향을 빼려고 홈플레이트 평면 좌표(m)의 변화를 px로 환산해 잰다.
    /// 이동 이벤트가 없는 프레임에 커서가 움직이면 idleMoved로 집계한다. 요약은 주기적으로 콘솔에 남긴다.
    /// </summary>
    public sealed class CursorTrace : IDisposable
    {
        private const float SummaryIntervalS = 2f;
        private const float MovedThresholdPx = 0.0001f;

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private StreamWriter _writer;
        private Vector2? _lastCursor;
        private int _frames;
        private int _idleFrames;
        private int _idleMoved;
        private float _idleMaxPx;
        private float _inputPx;
        private float _cursorPx;
        private float _nextSummary;

        public bool Enabled { get; private set; }

        public string FilePath { get; private set; }

        public void SetEnabled(bool enabled)
        {
            if (enabled == Enabled)
            {
                return;
            }

            Enabled = enabled;
            if (enabled)
            {
                string folder = Application.isEditor
                    ? Path.Combine(Application.dataPath, "..", "Logs")
                    : Application.persistentDataPath;
                Directory.CreateDirectory(folder);
                FilePath = Path.GetFullPath(Path.Combine(folder, "cursor_trace.csv"));
                _writer = new StreamWriter(FilePath, false);
                _writer.WriteLine("frame,time,moveEvents,eventDxPx,eventDyPx,cursorX,cursorZ,screenX,screenY,movedPx");
                ResetStats();
                Debug.Log("[CursorTrace] 기록 시작: " + FilePath);
            }
            else
            {
                LogSummary();
                Close();
            }
        }

        /// <summary>한 프레임 기록. 입력 처리 직후 호출한다</summary>
        public void Record(int frame, double time, int moveEvents, Vector2 eventDeltaPx, Vector2 cursor, Vector2 screen,
            float metersPerPixel, float sensitivity)
        {
            if (!Enabled)
            {
                return;
            }

            float moved = _lastCursor.HasValue && metersPerPixel > 0f
                ? (cursor - _lastCursor.Value).magnitude / metersPerPixel
                : 0f;
            _lastCursor = cursor;
            _frames++;
            if (moveEvents == 0)
            {
                _idleFrames++;
                if (moved > MovedThresholdPx)
                {
                    _idleMoved++;
                    _idleMaxPx = Mathf.Max(_idleMaxPx, moved);
                }
            }
            else
            {
                _inputPx += eventDeltaPx.magnitude * sensitivity;
                _cursorPx += moved;
            }

            _writer.WriteLine(string.Format(Invariant, "{0},{1:0.0000},{2},{3:0.###},{4:0.###},{5:0.00000},{6:0.00000},{7:0.###},{8:0.###},{9:0.####}",
                frame, time, moveEvents, eventDeltaPx.x, eventDeltaPx.y, cursor.x, cursor.y, screen.x, screen.y, moved));

            if (Time.unscaledTime >= _nextSummary)
            {
                LogSummary();
                _nextSummary = Time.unscaledTime + SummaryIntervalS;
            }
        }

        public void Dispose()
        {
            if (Enabled)
            {
                SetEnabled(false);
            }
        }

        private void LogSummary()
        {
            if (_frames == 0)
            {
                return;
            }

            // 입력이 있는 프레임: 커서 이동(px) / (이벤트 이동량 × 감도). 경계에 막히지 않으면 1.00이어야 한다
            string ratio = _inputPx > 0f ? (_cursorPx / _inputPx).ToString("0.00", Invariant) : "-";
            Debug.Log(string.Format(Invariant,
                "[CursorTrace] frames {0}, idle {1}, idle frames moved {2} (max {3:0.####} px), input-frame move ratio {4}",
                _frames, _idleFrames, _idleMoved, _idleMaxPx, ratio));
        }

        private void ResetStats()
        {
            _lastCursor = null;
            _frames = 0;
            _idleFrames = 0;
            _idleMoved = 0;
            _idleMaxPx = 0f;
            _inputPx = 0f;
            _cursorPx = 0f;
            _nextSummary = 0f;
        }

        private void Close()
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
