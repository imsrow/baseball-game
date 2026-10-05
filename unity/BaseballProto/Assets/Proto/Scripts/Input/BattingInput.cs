using System;
using BaseballProto.Core;
using BaseballProto.View;
using BaseballSim.Engine.Config;
using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 타격 입력. 커서 위치를 관리하고, 허용 구간 안의 첫 스윙 입력을 타임스탬프와 함께 잡아 둔다.
    /// 드래그 모드: 드래그 영역에서 시작한 손가락이 커서를 상대 이동, 스윙 버튼(또는 스페이스)을 누른 순간이 스윙.
    /// 탭 모드: 탭한 위치가 커서, 탭한 순간이 스윙.
    /// 홀드 모드: 누른 채 드래그로 커서 이동, 손을 뗀 순간이 스윙. 커서를 존 밖(테두리 + HoldTakeMarginM)으로 끌고 나가 떼면
    /// 스윙하지 않는다 (볼을 참는 방법). 다시 눌러 존 안에서 떼면 그 공에 스윙할 수 있다.
    /// 조작 방식은 화면 방향별로 따로 기억한다 (세로 기본 홀드, 가로 기본 드래그 패드 + 스윙 버튼).
    /// </summary>
    public sealed class BattingInput
    {
        private static readonly BattingControlMode[] CycleOrder =
        {
            BattingControlMode.HoldRelease, BattingControlMode.DragCursor, BattingControlMode.TapToSwing,
        };

        private readonly ProtoTuning _tuning;
        private readonly StrikeZoneConfig _zone;
        private readonly PlateMapper _mapper;
        private readonly Func<Vector2, bool> _isOnSwingButton;
        private readonly Func<Vector2, bool> _isInDragArea;

        private int _dragFinger = -1;
        private bool _armed;
        private double _armedFrom;
        private SwingInput? _swing;

        public BattingInput(ProtoTuning tuning, StrikeZoneConfig zone, PlateMapper mapper, Func<Vector2, bool> isOnSwingButton,
            Func<Vector2, bool> isInDragArea)
        {
            _tuning = tuning;
            _zone = zone;
            _mapper = mapper;
            _isOnSwingButton = isOnSwingButton;
            _isInDragArea = isInDragArea;
            CenterCursor();
        }

        /// <summary>현재 화면이 가로인지 (조작 방식 선택에 쓴다)</summary>
        public bool Landscape { get; set; }

        public BattingControlMode PortraitMode { get; set; } = BattingControlMode.HoldRelease;

        public BattingControlMode LandscapeMode { get; set; } = BattingControlMode.DragCursor;

        public BattingControlMode Mode => Landscape ? LandscapeMode : PortraitMode;

        /// <summary>커서 중심 (홈플레이트 평면, m)</summary>
        public Vector2 Cursor { get; private set; }

        /// <summary>탭 모드에서 마지막 탭 위치를 보여줄지</summary>
        public bool HasTapMark { get; private set; }

        /// <summary>홀드 모드에서 누르고 있는 중인지</summary>
        public bool Holding => Mode == BattingControlMode.HoldRelease && _dragFinger >= 0;

        /// <summary>홀드 모드에서 커서가 존 밖이라 손을 떼도 스윙하지 않는 상태인지</summary>
        public bool InTakeArea => Mode == BattingControlMode.HoldRelease && IsTakeArea(Cursor);

        /// <summary>현재 방향의 조작 방식을 다음 것으로 바꾼다</summary>
        public void CycleMode()
        {
            int index = Array.IndexOf(CycleOrder, Mode);
            BattingControlMode next = CycleOrder[(index + 1) % CycleOrder.Length];
            if (Landscape)
            {
                LandscapeMode = next;
            }
            else
            {
                PortraitMode = next;
            }

            _dragFinger = -1;
            HasTapMark = false;
        }

        public void CenterCursor()
        {
            Cursor = new Vector2(0f, (_tuning.CursorMinZM + _tuning.CursorMaxZM) * 0.5f);
        }

        /// <summary>이 시각 이후의 스윙 입력을 받는다</summary>
        public void Arm(double from)
        {
            _armed = true;
            _armedFrom = from;
            _swing = null;
        }

        public void Disarm()
        {
            _armed = false;
            _swing = null;
        }

        public bool TryTakeSwing(out SwingInput swing)
        {
            if (_swing.HasValue)
            {
                swing = _swing.Value;
                _swing = null;
                _armed = false;
                return true;
            }

            swing = default;
            return false;
        }

        public void Handle(PointerEvent e)
        {
            if (e.Phase == PointerPhase.SwingKey)
            {
                TrySwing(e.Time, Cursor);
                return;
            }

            switch (Mode)
            {
                case BattingControlMode.TapToSwing:
                    HandleTap(e);
                    break;
                case BattingControlMode.HoldRelease:
                    HandleHold(e);
                    break;
                default:
                    HandleDrag(e);
                    break;
            }
        }

        private void HandleTap(PointerEvent e)
        {
            if (e.Phase == PointerPhase.Down && _mapper.TryScreenToPlate(e.ScreenPosition, out Vector2 plate))
            {
                Cursor = Clamp(plate);
                HasTapMark = true;
                TrySwing(e.Time, Cursor);
            }
        }

        private void HandleHold(PointerEvent e)
        {
            HasTapMark = false;
            switch (e.Phase)
            {
                case PointerPhase.Down:
                    if (_dragFinger < 0)
                    {
                        _dragFinger = e.FingerId;
                    }

                    break;

                case PointerPhase.Move:
                    MoveCursor(e);
                    break;

                case PointerPhase.Up:
                    if (e.FingerId == _dragFinger)
                    {
                        _dragFinger = -1;

                        // 손을 뗀 순간(이벤트 타임스탬프)이 스윙 시각. 존 밖에서 떼면 스윙 취소
                        if (!IsTakeArea(Cursor))
                        {
                            TrySwing(e.Time, Cursor);
                        }
                    }

                    break;
            }
        }

        private void HandleDrag(PointerEvent e)
        {
            HasTapMark = false;
            switch (e.Phase)
            {
                case PointerPhase.Down:
                    if (_isOnSwingButton(e.ScreenPosition))
                    {
                        TrySwing(e.Time, Cursor);
                    }
                    else if (_dragFinger < 0 && _isInDragArea(e.ScreenPosition))
                    {
                        _dragFinger = e.FingerId;
                    }

                    break;

                case PointerPhase.Move:
                    MoveCursor(e);
                    break;

                case PointerPhase.Up:
                    if (e.FingerId == _dragFinger)
                    {
                        _dragFinger = -1;
                    }

                    break;
            }
        }

        private void MoveCursor(PointerEvent e)
        {
            if (e.FingerId != _dragFinger)
            {
                return;
            }

            float scale = _mapper.MetersPerPixel() * _tuning.DragSensitivity;
            Cursor = Clamp(Cursor + e.Delta * scale);
        }

        private void TrySwing(double time, Vector2 cursor)
        {
            if (!_armed || _swing.HasValue || time < _armedFrom)
            {
                return;
            }

            _swing = new SwingInput(time, cursor);
        }

        private bool IsTakeArea(Vector2 cursor)
        {
            float m = _tuning.HoldTakeMarginM;
            return Mathf.Abs(cursor.x) > (float)_zone.HalfWidthM + m
                || cursor.y < (float)_zone.BottomM - m
                || cursor.y > (float)_zone.TopM + m;
        }

        private Vector2 Clamp(Vector2 cursor)
        {
            return new Vector2(
                Mathf.Clamp(cursor.x, -_tuning.CursorLimitXM, _tuning.CursorLimitXM),
                Mathf.Clamp(cursor.y, _tuning.CursorMinZM, _tuning.CursorMaxZM));
        }
    }
}
