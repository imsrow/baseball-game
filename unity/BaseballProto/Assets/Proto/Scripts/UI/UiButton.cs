using System;
using UnityEngine;

namespace BaseballProto.UI
{
    /// <summary>
    /// HUD 버튼 (HUD 좌표). 입력은 터치 이벤트로 직접 판정하고 IMGUI는 그리기만 한다.
    /// </summary>
    public sealed class UiButton
    {
        public UiButton(Rect rect, string label, Action onPress, bool highlighted = false, bool enabled = true)
        {
            Rect = rect;
            Label = label;
            OnPress = onPress;
            Highlighted = highlighted;
            Enabled = enabled;
        }

        public Rect Rect { get; }

        public string Label { get; }

        public Action OnPress { get; }

        public bool Highlighted { get; }

        /// <summary>비활성: 흐리게 그리고, 눌러도 아무 일 없다 (터치는 HUD가 먹는다)</summary>
        public bool Enabled { get; }
    }
}
