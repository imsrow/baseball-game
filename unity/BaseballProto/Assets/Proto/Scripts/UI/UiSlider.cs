using System;
using UnityEngine;

namespace BaseballProto.UI
{
    /// <summary>
    /// HUD 슬라이더 (HUD 좌표). 막대 영역을 누르거나 끌면 값이 바뀐다.
    /// </summary>
    public sealed class UiSlider
    {
        private const float LabelRatio = 0.48f;

        private readonly Func<float> _get;
        private readonly Action<float> _set;

        public UiSlider(Rect rect, string label, string format, float min, float max, Func<float> get, Action<float> set)
        {
            Rect = rect;
            Label = label;
            Format = format;
            Min = min;
            Max = max;
            _get = get;
            _set = set;
        }

        public Rect Rect { get; }

        public string Label { get; }

        public string Format { get; }

        public float Min { get; }

        public float Max { get; }

        public float Value => _get();

        public Rect LabelRect => new Rect(Rect.x, Rect.y, Rect.width * LabelRatio, Rect.height);

        public Rect BarRect => new Rect(Rect.x + Rect.width * LabelRatio, Rect.y + Rect.height * 0.2f,
            Rect.width * (1f - LabelRatio), Rect.height * 0.6f);

        public float Normalized => Mathf.InverseLerp(Min, Max, Value);

        public void SetFromHud(Vector2 hudPoint)
        {
            Rect bar = BarRect;
            float t = Mathf.Clamp01((hudPoint.x - bar.x) / bar.width);
            _set(Mathf.Lerp(Min, Max, t));
        }
    }
}
