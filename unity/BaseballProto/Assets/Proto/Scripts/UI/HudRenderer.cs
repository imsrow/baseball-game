using System.Collections.Generic;
using BaseballProto.Core;
using BaseballProto.Input;
using BaseballSim.Engine.Events;
using UnityEngine;

namespace BaseballProto.UI
{
    /// <summary>
    /// HUD 그리기 (IMGUI, 그리기 전용). 입력은 Hud가 터치 이벤트로 처리한다.
    /// </summary>
    public sealed class HudRenderer
    {
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.12f, 0.16f, 0.85f);
        private static readonly Color ButtonOnColor = new Color(0.15f, 0.45f, 0.85f, 0.9f);
        private static readonly Color SwingColor = new Color(0.85f, 0.25f, 0.2f, 0.75f);
        private static readonly Color BarBackColor = new Color(1f, 1f, 1f, 0.2f);
        private static readonly Color BarFillColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        private static readonly Color SweetColor = new Color(0.2f, 1f, 0.3f, 0.6f);

        private const float DebugTop = 150f;
        private const float DebugLineHeight = 26f;

        private readonly Texture2D _white;
        private GUIStyle _label;
        private GUIStyle _center;
        private GUIStyle _headline;
        private int _styleFontBase;

        public HudRenderer()
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        public void Draw(Hud hud, double inputLagMs, float fps)
        {
            EnsureStyles();
            DuelController duel = hud.Duel;
            float w = UiScale.Width;
            float h = UiScale.Height;

            // 상단 상태
            Label(new Rect(10f, 80f, w - 20f, 34f), ResultText.Scoreboard(duel.Engine?.State), 24, _label);
            Label(new Rect(10f, 112f, w - 20f, 30f), duel.Matchup, 20, _label);

            // 디버그 (패널이 열려 있으면 가림)
            if (!hud.PanelRect.HasValue)
            {
                List<string> lines = DebugReadout.Build(duel, inputLagMs, fps);
                var panel = new Rect(6f, DebugTop - 4f, w - 12f, lines.Count * DebugLineHeight + 8f);
                Fill(panel, PanelColor);
                for (int i = 0; i < lines.Count; i++)
                {
                    Label(new Rect(12f, DebugTop + i * DebugLineHeight, w - 24f, DebugLineHeight), lines[i], 18, _label);
                }
            }
            else
            {
                Fill(hud.PanelRect.Value, PanelColor);
            }

            // 결과 문구
            if (ProtoClock.Now < duel.HeadlineUntil && !string.IsNullOrEmpty(duel.Headline))
            {
                Label(new Rect(0f, h * 0.36f, w, 90f), duel.Headline, 56, _headline);
            }

            foreach (UiSlider slider in hud.Sliders)
            {
                Label(slider.LabelRect, slider.Label + "  " + slider.Value.ToString(slider.Format), 20, _label);
                Rect bar = slider.BarRect;
                Fill(bar, BarBackColor);
                Fill(new Rect(bar.x, bar.y, bar.width * slider.Normalized, bar.height), BarFillColor);
            }

            foreach (UiButton button in hud.Buttons)
            {
                Fill(button.Rect, button.Highlighted ? ButtonOnColor : ButtonColor);
                Label(button.Rect, button.Label, 22, _center);
            }

            if (hud.SwingButtonVisible)
            {
                Fill(hud.SwingButtonRect, SwingColor);
                Label(hud.SwingButtonRect, "SWING", 36, _center);
            }

            DrawHint(hud, w, h);
            if (hud.GaugeVisible)
            {
                DrawGauge(hud);
            }
        }

        /// <summary>WebGL 첫 탭 대기 화면 (첫 탭에서 오디오가 켜진다)</summary>
        public void DrawStartOverlay()
        {
            EnsureStyles();
            float w = UiScale.Width;
            float h = UiScale.Height;
            Fill(new Rect(0f, 0f, w, h), PanelColor);
            Label(new Rect(0f, h * 0.40f, w, 90f), "TAP TO START", 60, _headline);
            Label(new Rect(0f, h * 0.40f + 100f, w, 40f), "Sound starts after the first tap", 24, _center);
            Label(new Rect(0f, h * 0.40f + 140f, w, 40f), "(iPhone silent switch mutes web audio)", 20, _center);
        }

        private void DrawHint(Hud hud, float w, float h)
        {
            string hint;
            if (hud.Duel.Mode == DuelMode.Batting)
            {
                hint = hud.Batting.Mode == BattingControlMode.DragCursor
                    ? "Drag: move cursor   SWING: hit (Space)"
                    : "Tap where & when the ball crosses";
            }
            else
            {
                switch (hud.Pitching.Stage)
                {
                    case PitchingStage.Aim: hint = "Pick pitch, tap zone to aim"; break;
                    case PitchingStage.Gauge: hint = "Tap to release (stop in green)"; break;
                    default: hint = string.Empty; break;
                }
            }

            float bottom = hud.SwingButtonVisible ? hud.SwingButtonRect.y - 40f : h - 30f;
            if (hud.GaugeVisible)
            {
                bottom = hud.GaugeRect.y - 44f;
            }

            Label(new Rect(10f, bottom, w - 20f, 34f), hint, 22, _label);
        }

        private void DrawGauge(Hud hud)
        {
            Rect r = hud.GaugeRect;
            ProtoTuning t = hud.Tuning;
            Fill(r, BarBackColor);
            float sweetHalf = t.GaugePerfectBand + (t.GaugeZeroBand - t.GaugePerfectBand) * 0.25f;
            float s0 = Mathf.Clamp01(t.GaugeSweetCenter - sweetHalf);
            float s1 = Mathf.Clamp01(t.GaugeSweetCenter + sweetHalf);
            Fill(new Rect(r.x + r.width * s0, r.y, r.width * (s1 - s0), r.height), SweetColor);

            PitchingInput p = hud.Pitching;
            if (p.Stage == PitchingStage.Gauge)
            {
                float v = p.GaugeValueAt(ProtoClock.Now);
                Fill(new Rect(r.x + r.width * v - 4f, r.y - 8f, 8f, r.height + 16f), Color.white);
            }
        }

        private void EnsureStyles()
        {
            int fontBase = UiScale.Font(100f);
            if (_label != null && fontBase == _styleFontBase)
            {
                return;
            }

            _styleFontBase = fontBase;
            _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, wordWrap = false };
            _label.normal.textColor = Color.white;
            _center = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter };
            _headline = new GUIStyle(_center) { fontStyle = FontStyle.Bold };
            _headline.normal.textColor = new Color(1f, 0.95f, 0.4f);
        }

        private void Label(Rect hudRect, string text, float size, GUIStyle style)
        {
            style.fontSize = UiScale.Font(size);
            GUI.Label(UiScale.ToGui(hudRect), text, style);
        }

        private void Fill(Rect hudRect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(UiScale.ToGui(hudRect), _white);
            GUI.color = previous;
        }
    }
}
