using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BaseballProto.Input
{
    /// <summary>
    /// 터치(멀티터치)와 스윙 키 입력을 타임스탬프와 함께 모은다.
    /// 프레임 시각이 아니라 입력 이벤트 시각을 그대로 보존하는 것이 목적이다.
    /// 에디터·PC(마우스)에서는 TouchSimulation으로 마우스를 터치로 바꾼다.
    ///
    /// 이동량은 Touch.delta를 쓰지 않고 손가락별 직전 위치와의 차이로 직접 계산한다.
    /// EnhancedTouch(Finger.OnTouchRecorded)는 프레임 내 누적 delta에서 "직전 기록의 delta"를 빼서 이벤트별 delta를 만드는데,
    /// 누적값은 매 프레임 0으로 초기화되므로 직전 기록이 이전 프레임 것이면 결과가 (이번 이동 − 직전 delta)가 된다.
    /// 그래서 프레임마다 이동이 오면 delta가 1, −2, 3, −4…처럼 진동한다 (에디터·실기기 공통, 기기 종류와 무관).
    ///
    /// 에디터에서는 Input System 에디터 플러그인이 도메인 리로드 직후 사용자 설정(Simulate Touch Input)에 맞춰
    /// TouchSimulation을 끌 수 있으므로, 매 프레임 꺼져 있으면 다시 켠다.
    /// </summary>
    public sealed class TouchHub : IDisposable
    {
        private readonly List<PointerEvent> _queue = new List<PointerEvent>();
        private readonly Dictionary<int, Vector2> _lastPositions = new Dictionary<int, Vector2>();
        private readonly InputAction _swingKey;
        private readonly bool _simulated;

        public TouchHub()
        {
            EnhancedTouchSupport.Enable();
            _simulated = Application.isEditor || !Application.isMobilePlatform;
            if (_simulated)
            {
                TouchSimulation.Enable();
            }

            ETouch.onFingerDown += OnFingerDown;
            ETouch.onFingerMove += OnFingerMove;
            ETouch.onFingerUp += OnFingerUp;

            _swingKey = new InputAction("SwingKey", InputActionType.Button, "<Keyboard>/space");
            _swingKey.performed += OnSwingKey;
            _swingKey.Enable();
        }

        /// <summary>마지막 이벤트의 입력 지연 (현재 시각 − 이벤트 시각, ms). 디버그 표시용</summary>
        public double LastEventLagMs { get; private set; }

        /// <summary>쌓인 이벤트를 꺼낸다 (발생 순서)</summary>
        public void Drain(List<PointerEvent> into)
        {
            if (_simulated && (TouchSimulation.instance == null || !TouchSimulation.instance.enabled))
            {
                TouchSimulation.Enable();
            }

            into.AddRange(_queue);
            _queue.Clear();
        }

        public void Dispose()
        {
            ETouch.onFingerDown -= OnFingerDown;
            ETouch.onFingerMove -= OnFingerMove;
            ETouch.onFingerUp -= OnFingerUp;
            _swingKey.performed -= OnSwingKey;
            _swingKey.Disable();
            _swingKey.Dispose();
            if (_simulated)
            {
                TouchSimulation.Disable();
            }

            EnhancedTouchSupport.Disable();
        }

        private void OnFingerDown(Finger finger)
        {
            ETouch touch = finger.currentTouch;
            _lastPositions[finger.index] = touch.screenPosition;
            Add(new PointerEvent(finger.index, PointerPhase.Down, touch.screenPosition, Vector2.zero, touch.startTime));
        }

        private void OnFingerMove(Finger finger)
        {
            ETouch touch = finger.currentTouch;
            Vector2 position = touch.screenPosition;
            Vector2 delta = _lastPositions.TryGetValue(finger.index, out Vector2 last) ? position - last : Vector2.zero;
            _lastPositions[finger.index] = position;
            if (delta == Vector2.zero)
            {
                return;
            }

            Add(new PointerEvent(finger.index, PointerPhase.Move, position, delta, touch.time));
        }

        private void OnFingerUp(Finger finger)
        {
            ETouch touch = finger.currentTouch;
            _lastPositions.Remove(finger.index);
            Add(new PointerEvent(finger.index, PointerPhase.Up, touch.screenPosition, Vector2.zero, touch.time));
        }

        private void OnSwingKey(InputAction.CallbackContext context)
        {
            Add(PointerEvent.Key(context.time));
        }

        private void Add(PointerEvent e)
        {
            LastEventLagMs = (Core.ProtoClock.Now - e.Time) * 1000.0;
            _queue.Add(e);
        }
    }
}
