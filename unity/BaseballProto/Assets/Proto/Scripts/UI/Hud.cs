using System.Collections.Generic;
using BaseballProto.Core;
using BaseballProto.Feedback;
using BaseballProto.Input;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using UnityEngine;

namespace BaseballProto.UI
{
    /// <summary>
    /// HUD 배치와 입력 판정. 매 프레임 Layout()으로 위젯을 다시 만들고, 터치는 Handle()에서 먼저 받는다.
    /// HUD가 쓰지 않은 터치만 게임 입력(타격·투구)으로 넘어간다. 그리기는 HudRenderer가 한다.
    /// 배치는 화면 방향(세로/가로)과 안전 영역(노치·홈 인디케이터 제외)을 기준으로 매 프레임 계산하므로
    /// 경기 중에 방향을 바꿔도 진행 상태와 무관하게 바로 따라간다.
    /// </summary>
    public sealed class Hud
    {
        private const float Margin = 10f;
        private const float TopBarHeightPortrait = 64f;
        private const float TopBarHeightLandscape = 56f;
        private const float InfoBlockHeight = 92f;
        private const float SliderRowHeight = 52f;
        private const float FxRowHeight = 60f;
        private const float SwingButtonSize = 230f;
        private const float DragPadSize = 280f;
        private const float DragPadSlack = 40f;
        private const float StartButtonWidth = 300f;
        private const float StartButtonHeight = 100f;

        // 세로: 존 아래·스윙 버튼 위 사이 (화면 높이 비율)
        private const float PortraitStartY = 0.71f;
        private const float PitchButtonHeight = 72f;
        private const float PitchButtonMaxWidth = 170f;
        private const float PitchButtonGap = 8f;
        private const float GaugeHeight = 44f;
        private const float LandscapeDebugMaxWidth = 620f;
        private const float LandscapeDebugRatio = 0.48f;
        private const float LandscapePanelMaxWidth = 640f;

        private readonly DuelController _duel;
        private readonly BattingInput _batting;
        private readonly PitchingInput _pitching;
        private readonly ProtoTuning _tuning;
        private readonly ImpactFeedback _impact;
        private readonly CursorTrace _trace;
        private readonly Dictionary<int, UiSlider> _capturedSliders = new Dictionary<int, UiSlider>();
        private readonly HashSet<int> _consumedFingers = new HashSet<int>();

        public Hud(DuelController duel, BattingInput batting, PitchingInput pitching, ProtoTuning tuning, ImpactFeedback impact,
            CursorTrace trace)
        {
            _duel = duel;
            _batting = batting;
            _pitching = pitching;
            _tuning = tuning;
            _impact = impact;
            _trace = trace;
        }

        public List<UiButton> Buttons { get; } = new List<UiButton>();

        public List<UiSlider> Sliders { get; } = new List<UiSlider>();

        public bool Landscape { get; private set; }

        public bool TuneOpen { get; private set; }

        public bool FxOpen { get; private set; }

        /// <summary>점수판·대결·이번 경기 성적 표시 영역</summary>
        public Rect InfoRect { get; private set; }

        /// <summary>디버그 표시 시작 위치·폭 (높이는 줄 수에 따라)</summary>
        public Rect DebugRect { get; private set; }

        /// <summary>열린 패널 배경 (게임 입력 차단 영역)</summary>
        public Rect? PanelRect { get; private set; }

        public Rect SwingButtonRect { get; private set; }

        public bool SwingButtonVisible { get; private set; }

        /// <summary>가로 드래그 모드의 커서 패드</summary>
        public Rect DragPadRect { get; private set; }

        public bool DragPadVisible { get; private set; }

        public Rect GaugeRect { get; private set; }

        public bool GaugeVisible { get; private set; }

        public Rect HintRect { get; private set; }

        public float HeadlineY { get; private set; }

        public DuelController Duel => _duel;

        public PitchingInput Pitching => _pitching;

        public BattingInput Batting => _batting;

        public ProtoTuning Tuning => _tuning;

        /// <summary>드래그 모드 스윙 버튼 위인지 (화면 좌표)</summary>
        public bool IsOnSwingButton(Vector2 screen)
        {
            return SwingButtonVisible && SwingButtonRect.Contains(UiScale.FromScreen(screen));
        }

        /// <summary>드래그 모드에서 커서 드래그를 시작할 수 있는 곳인지 (가로: 패드 주변만, 세로: 어디든)</summary>
        public bool IsInDragArea(Vector2 screen)
        {
            if (!DragPadVisible)
            {
                return true;
            }

            Rect pad = DragPadRect;
            var slack = new Rect(pad.x - DragPadSlack, pad.y - DragPadSlack, pad.width + DragPadSlack * 2f,
                pad.height + DragPadSlack * 2f);
            return slack.Contains(UiScale.FromScreen(screen));
        }

        public void Layout()
        {
            Buttons.Clear();
            Sliders.Clear();
            Landscape = UiScale.IsLandscape;
            _batting.Landscape = Landscape;
            Rect safe = UiScale.Safe;
            bool batting = _duel.Mode == DuelMode.Batting;

            float topH = Landscape ? TopBarHeightLandscape : TopBarHeightPortrait;
            float top = safe.y + Margin;
            float modeW = Landscape ? 170f : 200f;
            float ctrlW = Landscape ? 190f : 210f;
            float smallW = Landscape ? 120f : 135f;

            Buttons.Add(new UiButton(new Rect(safe.x + Margin, top, modeW, topH), batting ? "MODE: BAT" : "MODE: PITCH",
                () => _duel.RequestMode(batting ? DuelMode.Pitching : DuelMode.Batting)));
            if (batting)
            {
                Buttons.Add(new UiButton(new Rect(safe.x + Margin * 2f + modeW, top, ctrlW, topH), "CTRL: " + ModeName(_batting.Mode),
                    _batting.CycleMode));
            }

            Buttons.Add(new UiButton(new Rect(safe.xMax - Margin * 2f - smallW * 2f, top, smallW, topH), "TUNE", ToggleTune, TuneOpen));
            Buttons.Add(new UiButton(new Rect(safe.xMax - Margin - smallW, top, smallW, topH), "FX", ToggleFx, FxOpen));

            float infoTop = top + topH + 8f;
            float infoWidth = Landscape ? safe.width * 0.5f : safe.width - Margin * 2f;
            InfoRect = new Rect(safe.x + Margin, infoTop, infoWidth, InfoBlockHeight);
            float debugTop = infoTop + InfoBlockHeight + 4f;
            float debugWidth = Landscape
                ? Mathf.Min(LandscapeDebugMaxWidth, safe.width * LandscapeDebugRatio)
                : safe.width - Margin * 2f;
            DebugRect = new Rect(safe.x + Margin, debugTop, debugWidth, 0f);
            HeadlineY = safe.y + safe.height * (Landscape ? 0.30f : 0.36f);

            PanelRect = null;
            if (TuneOpen)
            {
                LayoutTune(safe, debugTop);
            }
            else if (FxOpen)
            {
                LayoutFx(safe, debugTop);
            }

            LayoutControls(safe, batting);
        }

        /// <summary>HUD가 이 이벤트를 썼으면 true (게임 입력으로 넘기지 않음)</summary>
        public bool Handle(PointerEvent e)
        {
            if (e.Phase == PointerPhase.SwingKey)
            {
                return false;
            }

            Vector2 p = UiScale.FromScreen(e.ScreenPosition);
            switch (e.Phase)
            {
                case PointerPhase.Down:
                    foreach (UiSlider slider in Sliders)
                    {
                        if (slider.Rect.Contains(p))
                        {
                            _capturedSliders[e.FingerId] = slider;
                            slider.SetFromHud(p);
                            return true;
                        }
                    }

                    foreach (UiButton button in Buttons)
                    {
                        if (button.Rect.Contains(p))
                        {
                            button.OnPress();
                            _consumedFingers.Add(e.FingerId);
                            return true;
                        }
                    }

                    if (PanelRect.HasValue && PanelRect.Value.Contains(p))
                    {
                        _consumedFingers.Add(e.FingerId);
                        return true;
                    }

                    return false;

                case PointerPhase.Move:
                    if (_capturedSliders.TryGetValue(e.FingerId, out UiSlider captured))
                    {
                        captured.SetFromHud(p);
                        return true;
                    }

                    return _consumedFingers.Contains(e.FingerId);

                default:
                    bool used = _capturedSliders.Remove(e.FingerId) | _consumedFingers.Remove(e.FingerId);
                    return used;
            }
        }

        public static string ModeName(BattingControlMode mode)
        {
            switch (mode)
            {
                case BattingControlMode.HoldRelease: return "HOLD";
                case BattingControlMode.TapToSwing: return "TAP";
                default: return "DRAG";
            }
        }

        private void LayoutControls(Rect safe, bool batting)
        {
            bool drag = batting && _batting.Mode == BattingControlMode.DragCursor;
            SwingButtonVisible = drag;
            DragPadVisible = drag && Landscape;
            GaugeVisible = false;

            float swingSize = Landscape ? SwingButtonSize + 20f : SwingButtonSize;
            SwingButtonRect = new Rect(safe.xMax - swingSize - 30f, safe.yMax - swingSize - 20f, swingSize, swingSize);
            DragPadRect = new Rect(safe.x + 30f, safe.yMax - DragPadSize - 20f, DragPadSize, DragPadSize);

            float hintY = safe.yMax - 44f;
            if (SwingButtonVisible && !Landscape)
            {
                hintY = SwingButtonRect.y - 44f;
            }

            if (!batting && (_pitching.Stage == PitchingStage.Aim || _pitching.Stage == PitchingStage.Gauge))
            {
                hintY = LayoutPitchButtons(safe) - 44f;
            }

            if (_duel.AwaitingReady)
            {
                float startY = Landscape ? safe.yMax - StartButtonHeight - 30f : safe.y + safe.height * PortraitStartY;
                var start = new Rect(safe.center.x - StartButtonWidth * 0.5f, startY, StartButtonWidth, StartButtonHeight);
                Buttons.Add(new UiButton(start, "START", _duel.RequestPitch, true));
                if (Landscape)
                {
                    hintY = start.y - 44f;
                }
            }

            float hintX = DragPadVisible ? DragPadRect.xMax + Margin : safe.x + Margin;
            float hintW = (SwingButtonVisible && Landscape ? SwingButtonRect.x : safe.xMax - Margin) - hintX;
            HintRect = new Rect(hintX, hintY, Mathf.Max(100f, hintW), 34f);
        }

        /// <returns>버튼·게이지 영역의 맨 위 y</returns>
        private float LayoutPitchButtons(Rect safe)
        {
            IReadOnlyList<PitchType> types = _pitching.Repertoire;
            int perRow = Landscape ? Mathf.Max(1, types.Count) : 4;
            int rows = (types.Count + perRow - 1) / perRow;
            float avail = safe.width - Margin * 2f;
            float width = Mathf.Min(PitchButtonMaxWidth, (avail - PitchButtonGap * (perRow - 1)) / perRow);
            float rowWidth = width * perRow + PitchButtonGap * (perRow - 1);
            float left = safe.center.x - rowWidth * 0.5f;
            float top = safe.yMax - 20f - rows * (PitchButtonHeight + PitchButtonGap);
            for (int i = 0; i < types.Count; i++)
            {
                PitchType type = types[i];
                int row = i / perRow;
                int col = i % perRow;
                var rect = new Rect(left + col * (width + PitchButtonGap), top + row * (PitchButtonHeight + PitchButtonGap),
                    width, PitchButtonHeight);
                Buttons.Add(new UiButton(rect, ResultText.PitchName(type), () => _pitching.SelectType(type),
                    _pitching.SelectedType == type));
            }

            GaugeVisible = true;
            float gaugeWidth = Mathf.Min(rowWidth, safe.width - 80f);
            GaugeRect = new Rect(safe.center.x - gaugeWidth * 0.5f, top - GaugeHeight - 24f, gaugeWidth, GaugeHeight);
            return GaugeRect.y;
        }

        private void ToggleTune()
        {
            TuneOpen = !TuneOpen;
            FxOpen = false;
        }

        private void ToggleFx()
        {
            FxOpen = !FxOpen;
            TuneOpen = false;
        }

        private void LayoutTune(Rect safe, float panelTop)
        {
            InputModifierConfig m = _duel.Config.InputModifier;
            ProtoTuning t = _tuning;
            int columns = Landscape ? 2 : 1;
            float colWidth = (safe.width - Margin * (columns + 1)) / columns;
            int index = 0;
            int rowsPerColumn = 0;
            var rows = new List<System.Action<Rect>>();

            void Row(string label, string format, float min, float max, System.Func<float> get, System.Action<float> set)
            {
                rows.Add(r => Sliders.Add(new UiSlider(r, label, format, min, max, get, set)));
            }

            // 엔진 보정 상한: 캐주얼 손맛 실험용으로 기본값보다 훨씬 넓게
            Row("Contact logit", "0.00", 0f, 3f, () => (float)m.MaxSwingContactLogitShift, v => m.MaxSwingContactLogitShift = v);
            Row("Solid logit", "0.00", 0f, 3f, () => (float)m.MaxSwingSolidLogitShift, v => m.MaxSwingSolidLogitShift = v);
            Row("Spray deg", "0.0", 0f, 45f, () => (float)m.MaxTimingSprayShiftDeg, v => m.MaxTimingSprayShiftDeg = v);
            Row("Launch deg", "0.0", 0f, 40f, () => (float)m.MaxCursorLaunchAngleShiftDeg, v => m.MaxCursorLaunchAngleShiftDeg = v);
            Row("Release sigma", "0.00", 0f, 1.5f, () => (float)m.MaxPitchExecutionSigmaLogShift,
                v => m.MaxPitchExecutionSigmaLogShift = v);

            // 프로토 조작 계수
            Row("Ball slow x", "0.00", 1f, 2.5f, () => t.FlightTimeScale, v => t.FlightTimeScale = v);
            Row("Perfect ms", "0", 5f, 80f, () => t.PerfectTimingMs, v => t.PerfectTimingMs = v);
            Row("Zero ms", "0", 60f, 250f, () => t.ZeroTimingMs, v => t.ZeroTimingMs = v);
            Row("Calib ms", "0", -50f, 150f, () => t.DisplayLatencyMs, v => t.DisplayLatencyMs = v);
            Row("Cursor R m", "0.000", 0.05f, 0.25f, () => t.CursorRadiusM, v => t.CursorRadiusM = v);
            Row("Timing weight", "0.00", 0f, 1f, () => t.TimingWeight, v => t.TimingWeight = v);
            Row("Cursor zero d/R", "0.00", 0.5f, 4f, () => t.CursorZeroRatio,
                v => t.CursorZeroRatio = Mathf.Max(v, t.CursorPerfectRatio + 0.05f));
            Row("Cursor perfect d/R", "0.00", 0f, 1.5f, () => t.CursorPerfectRatio,
                v => t.CursorPerfectRatio = Mathf.Min(v, t.CursorZeroRatio - 0.05f));
            Row("Drag sens", "0.00", 0.3f, 3f, () => t.DragSensitivity, v => t.DragSensitivity = v);
            Row("Gauge period s", "0.00", 0.5f, 2.5f, () => t.GaugePeriodS, v => t.GaugePeriodS = v);

            rowsPerColumn = (rows.Count + columns - 1) / columns;
            foreach (System.Action<Rect> add in rows)
            {
                int col = index / rowsPerColumn;
                int row = index % rowsPerColumn;
                add(new Rect(safe.x + Margin + col * (colWidth + Margin), panelTop + row * SliderRowHeight, colWidth,
                    SliderRowHeight));
                index++;
            }

            float bottom = panelTop + rowsPerColumn * SliderRowHeight;
            Buttons.Add(new UiButton(new Rect(safe.x + Margin, bottom + 6f, 200f, TopBarHeightLandscape), "RESET", ResetTuning));
            PanelRect = new Rect(0f, panelTop - 6f, UiScale.Width, bottom - panelTop + TopBarHeightLandscape + 18f);
        }

        private void LayoutFx(Rect safe, float panelTop)
        {
            FeedbackSettings s = _impact.Settings;
            float y = panelTop;
            float width = Landscape ? Mathf.Min(LandscapePanelMaxWidth, safe.width - Margin * 2f) : safe.width - Margin * 2f;
            float x = safe.x + Margin;

            void Add(UiButton button)
            {
                Buttons.Add(button);
                y += FxRowHeight + 6f;
            }

            Add(_impact.HapticsSupported
                ? Toggle(new Rect(x, y, width, FxRowHeight), "Vibration", s.Vibration, () => s.Vibration = !s.Vibration)
                : new UiButton(new Rect(x, y, width, FxRowHeight), "Vibration: n/a on this platform", () => { }));
            Add(Toggle(new Rect(x, y, width, FxRowHeight), "Sound", s.Sound, () => s.Sound = !s.Sound));
            Add(Toggle(new Rect(x, y, width, FxRowHeight), "Hit stop", s.HitStop, () => s.HitStop = !s.HitStop));
            Add(Toggle(new Rect(x, y, width, FxRowHeight), "Screen shake", s.Shake, () => s.Shake = !s.Shake));
            Add(Toggle(new Rect(x, y, width, FxRowHeight), "Cursor log (csv)", _trace.Enabled,
                () => _trace.SetEnabled(!_trace.Enabled)));
            PanelRect = new Rect(0f, panelTop - 6f, x + width + Margin, y - panelTop + 12f);
        }

        private static UiButton Toggle(Rect rect, string label, bool on, System.Action flip)
        {
            return new UiButton(rect, label + ": " + (on ? "ON" : "OFF"), flip, on);
        }

        private void ResetTuning()
        {
            var defaults = new InputModifierConfig();
            InputModifierConfig m = _duel.Config.InputModifier;
            m.MaxSwingContactLogitShift = defaults.MaxSwingContactLogitShift;
            m.MaxSwingSolidLogitShift = defaults.MaxSwingSolidLogitShift;
            m.MaxTimingSprayShiftDeg = defaults.MaxTimingSprayShiftDeg;
            m.MaxCursorLaunchAngleShiftDeg = defaults.MaxCursorLaunchAngleShiftDeg;
            m.MaxPitchExecutionSigmaLogShift = defaults.MaxPitchExecutionSigmaLogShift;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new ProtoTuning()), _tuning);
        }
    }
}
