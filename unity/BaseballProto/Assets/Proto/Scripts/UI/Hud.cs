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
    /// </summary>
    public sealed class Hud
    {
        private const float Margin = 10f;
        private const float TopBarHeight = 64f;
        private const float PanelTop = 150f;
        private const float SliderRowHeight = 56f;
        private const float SwingButtonSize = 230f;
        private const float PitchButtonHeight = 72f;
        private const float PitchButtonGap = 8f;
        private const int PitchButtonsPerRow = 4;
        private const float GaugeHeight = 44f;

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
            _trace = trace;
            _duel = duel;
            _batting = batting;
            _pitching = pitching;
            _tuning = tuning;
            _impact = impact;
        }

        public List<UiButton> Buttons { get; } = new List<UiButton>();

        public List<UiSlider> Sliders { get; } = new List<UiSlider>();

        public bool TuneOpen { get; private set; }

        public bool FxOpen { get; private set; }

        /// <summary>열린 패널 배경 (게임 입력 차단 영역)</summary>
        public Rect? PanelRect { get; private set; }

        public Rect SwingButtonRect { get; private set; }

        public bool SwingButtonVisible { get; private set; }

        public Rect GaugeRect { get; private set; }

        public bool GaugeVisible { get; private set; }

        public DuelController Duel => _duel;

        public PitchingInput Pitching => _pitching;

        public BattingInput Batting => _batting;

        public ProtoTuning Tuning => _tuning;

        /// <summary>드래그 모드 스윙 버튼 위인지 (화면 좌표)</summary>
        public bool IsOnSwingButton(Vector2 screen)
        {
            return SwingButtonVisible && SwingButtonRect.Contains(UiScale.FromScreen(screen));
        }

        public void Layout()
        {
            Buttons.Clear();
            Sliders.Clear();
            float w = UiScale.Width;
            float h = UiScale.Height;
            bool batting = _duel.Mode == DuelMode.Batting;

            Buttons.Add(new UiButton(new Rect(Margin, Margin, 200f, TopBarHeight), batting ? "MODE: BAT" : "MODE: PITCH",
                () => _duel.RequestMode(batting ? DuelMode.Pitching : DuelMode.Batting)));
            if (batting)
            {
                bool drag = _batting.Mode == BattingControlMode.DragCursor;
                Buttons.Add(new UiButton(new Rect(220f, Margin, 200f, TopBarHeight), drag ? "CTRL: DRAG" : "CTRL: TAP",
                    () => _batting.Mode = drag ? BattingControlMode.TapToSwing : BattingControlMode.DragCursor));
            }

            Buttons.Add(new UiButton(new Rect(w - 290f, Margin, 135f, TopBarHeight), "TUNE", ToggleTune, TuneOpen));
            Buttons.Add(new UiButton(new Rect(w - 145f, Margin, 135f, TopBarHeight), "FX", ToggleFx, FxOpen));

            PanelRect = null;
            if (TuneOpen)
            {
                LayoutTune(w);
            }
            else if (FxOpen)
            {
                LayoutFx(w);
            }

            SwingButtonVisible = batting && _batting.Mode == BattingControlMode.DragCursor;
            SwingButtonRect = new Rect(w - SwingButtonSize - 20f, h - SwingButtonSize - 40f, SwingButtonSize, SwingButtonSize);

            GaugeVisible = false;
            if (!batting && (_pitching.Stage == PitchingStage.Aim || _pitching.Stage == PitchingStage.Gauge))
            {
                LayoutPitchButtons(w, h);
            }
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

        private void LayoutTune(float w)
        {
            InputModifierConfig m = _duel.Config.InputModifier;
            ProtoTuning t = _tuning;
            float y = PanelTop;
            float rowWidth = w - Margin * 2f;

            void Row(string label, string format, float min, float max, System.Func<float> get, System.Action<float> set)
            {
                Sliders.Add(new UiSlider(new Rect(Margin, y, rowWidth, SliderRowHeight), label, format, min, max, get, set));
                y += SliderRowHeight;
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

            Buttons.Add(new UiButton(new Rect(Margin, y + 6f, 200f, TopBarHeight), "RESET", ResetTuning));
            PanelRect = new Rect(0f, PanelTop - 6f, w, y - PanelTop + TopBarHeight + 18f);
        }

        private void LayoutFx(float w)
        {
            FeedbackSettings s = _impact.Settings;
            float y = PanelTop;
            float width = w - Margin * 2f;
            if (_impact.HapticsSupported)
            {
                Buttons.Add(Toggle(new Rect(Margin, y, width, TopBarHeight), "Vibration", s.Vibration, () => s.Vibration = !s.Vibration));
            }
            else
            {
                Buttons.Add(new UiButton(new Rect(Margin, y, width, TopBarHeight), "Vibration: n/a on this platform", () => { }));
            }

            y += TopBarHeight + 8f;
            Buttons.Add(Toggle(new Rect(Margin, y, width, TopBarHeight), "Sound", s.Sound, () => s.Sound = !s.Sound));
            y += TopBarHeight + 8f;
            Buttons.Add(Toggle(new Rect(Margin, y, width, TopBarHeight), "Hit stop", s.HitStop, () => s.HitStop = !s.HitStop));
            y += TopBarHeight + 8f;
            Buttons.Add(Toggle(new Rect(Margin, y, width, TopBarHeight), "Screen shake", s.Shake, () => s.Shake = !s.Shake));
            y += TopBarHeight + 8f;
            Buttons.Add(Toggle(new Rect(Margin, y, width, TopBarHeight), "Cursor log (csv)", _trace.Enabled,
                () => _trace.SetEnabled(!_trace.Enabled)));
            y += TopBarHeight + 8f;
            PanelRect = new Rect(0f, PanelTop - 6f, w, y - PanelTop + 12f);
        }

        private static UiButton Toggle(Rect rect, string label, bool on, System.Action flip)
        {
            return new UiButton(rect, label + ": " + (on ? "ON" : "OFF"), flip, on);
        }

        private void LayoutPitchButtons(float w, float h)
        {
            IReadOnlyList<PitchType> types = _pitching.Repertoire;
            int rows = (types.Count + PitchButtonsPerRow - 1) / PitchButtonsPerRow;
            float width = (w - Margin * 2f - PitchButtonGap * (PitchButtonsPerRow - 1)) / PitchButtonsPerRow;
            float top = h - 30f - rows * (PitchButtonHeight + PitchButtonGap);
            for (int i = 0; i < types.Count; i++)
            {
                PitchType type = types[i];
                int row = i / PitchButtonsPerRow;
                int col = i % PitchButtonsPerRow;
                var rect = new Rect(Margin + col * (width + PitchButtonGap), top + row * (PitchButtonHeight + PitchButtonGap),
                    width, PitchButtonHeight);
                Buttons.Add(new UiButton(rect, ResultText.PitchName(type), () => _pitching.SelectType(type),
                    _pitching.SelectedType == type));
            }

            GaugeVisible = true;
            GaugeRect = new Rect(40f, top - GaugeHeight - 24f, w - 80f, GaugeHeight);
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
