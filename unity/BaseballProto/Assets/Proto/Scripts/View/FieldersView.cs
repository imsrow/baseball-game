using System.Collections.Generic;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 수비수 9명. 기본 위치는 엔진 FieldConfig 그대로.
    /// 투구 중에는 투수(FieldView의 투수 큐브)와 포수(카메라 바로 앞이라 가림)를 숨기고, 인플레이 연출 때만 보인다.
    /// </summary>
    public sealed class FieldersView
    {
        private readonly Dictionary<Position, FigureView> _figures = new Dictionary<Position, FigureView>();
        private readonly Dictionary<Position, Vector3> _starts = new Dictionary<Position, Vector3>();

        public FieldersView(Transform parent, FieldGeometry field)
        {
            foreach (Position p in PositionInfo.Fielding)
            {
                FieldPoint start = field.FielderStart(p);
                _starts[p] = new Vector3((float)start.X, 0f, (float)start.Y);
                _figures[p] = new FigureView(parent, "Fielder " + PositionInfo.Abbreviation(p), ProtoColors.Fielder);
            }

            ResetForPitch();
        }

        public FigureView Get(Position position)
        {
            return _figures[position];
        }

        /// <summary>투구 대기: 기본 위치, 투수·포수 숨김</summary>
        public void ResetForPitch()
        {
            foreach (KeyValuePair<Position, FigureView> pair in _figures)
            {
                pair.Value.Snap(_starts[pair.Key], Vector3.zero);
                pair.Value.Visible = pair.Key != Position.Pitcher && pair.Key != Position.Catcher;
            }
        }

        /// <summary>인플레이 연출 시작: 투수는 마운드에서. 포수는 SetCatcherVisible로 (타석 시점 카메라 바로 앞이라 가린다)</summary>
        public void ShowForPlay()
        {
            _figures[Position.Pitcher].Visible = true;
        }

        public void SetCatcherVisible(bool visible)
        {
            _figures[Position.Catcher].Visible = visible;
        }
    }
}
