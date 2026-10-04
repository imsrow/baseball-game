using System;
using BaseballProto.Core;
using BaseballProto.View;
using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 타격 입력. 커서 위치를 관리하고, 허용 구간 안의 첫 스윙 입력을 타임스탬프와 함께 잡아 둔다.
    /// 드래그 모드: 한 손가락은 커서를 상대 이동, 스윙 버튼(또는 스페이스)을 누른 순간이 스윙.
    /// 탭 모드: 탭한 위치가 커서, 탭한 순간이 스윙.
    /// </summary>
    public sealed class BattingInput
    {
        private readonly ProtoTuning _tuning;
        private readonly PlateMapper _mapper;
        private readonly Func<Vector2, bool> _isOnSwingButton;

        private int _dragFinger = -1;
        private bool _armed;
        private double _armedFrom;
        private SwingInput? _swing;

        public BattingInput(ProtoTuning tuning, PlateMapper mapper, Func<Vector2, bool> isOnSwingButton)
        {
            _tuning = tuning;
            _mapper = mapper;
            _isOnSwingButton = isOnSwingButton;
            CenterCursor();
        }

        public BattingControlMode Mode { get; set; } = BattingControlMode.DragCursor;

        /// <summary>커서 중심 (홈플레이트 평면, m)</summary>
        public Vector2 Cursor { get; private set; }

        /// <summary>탭 모드에서 마지막 탭 위치를 보여줄지</summary>
        public bool HasTapMark { get; private set; }

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

            if (Mode == BattingControlMode.TapToSwing)
            {
                if (e.Phase == PointerPhase.Down && _mapper.TryScreenToPlate(e.ScreenPosition, out Vector2 plate))
                {
                    Cursor = Clamp(plate);
                    HasTapMark = true;
                    TrySwing(e.Time, Cursor);
                }

                return;
            }

            HasTapMark = false;
            switch (e.Phase)
            {
                case PointerPhase.Down:
                    if (_isOnSwingButton(e.ScreenPosition))
                    {
                        TrySwing(e.Time, Cursor);
                    }
                    else if (_dragFinger < 0)
                    {
                        _dragFinger = e.FingerId;
                    }

                    break;

                case PointerPhase.Move:
                    if (e.FingerId == _dragFinger)
                    {
                        float scale = _mapper.MetersPerPixel() * _tuning.DragSensitivity;
                        Cursor = Clamp(Cursor + e.Delta * scale);
                    }

                    break;

                case PointerPhase.Up:
                    if (e.FingerId == _dragFinger)
                    {
                        _dragFinger = -1;
                    }

                    break;
            }
        }

        private void TrySwing(double time, Vector2 cursor)
        {
            if (!_armed || _swing.HasValue || time < _armedFrom)
            {
                return;
            }

            _swing = new SwingInput(time, cursor);
        }

        private Vector2 Clamp(Vector2 cursor)
        {
            return new Vector2(
                Mathf.Clamp(cursor.x, -_tuning.CursorLimitXM, _tuning.CursorLimitXM),
                Mathf.Clamp(cursor.y, _tuning.CursorMinZM, _tuning.CursorMaxZM));
        }
    }
}
