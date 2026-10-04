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
    /// </summary>
    public sealed class TouchHub : IDisposable
    {
        private readonly List<PointerEvent> _queue = new List<PointerEvent>();
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
            Add(new PointerEvent(finger.index, PointerPhase.Down, touch.screenPosition, Vector2.zero, touch.startTime));
        }

        private void OnFingerMove(Finger finger)
        {
            ETouch touch = finger.currentTouch;
            Add(new PointerEvent(finger.index, PointerPhase.Move, touch.screenPosition, touch.delta, touch.time));
        }

        private void OnFingerUp(Finger finger)
        {
            ETouch touch = finger.currentTouch;
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
