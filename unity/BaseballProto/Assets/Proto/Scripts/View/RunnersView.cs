using System.Collections.Generic;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.State;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 주자 표시. 투구 사이에는 현재 루상 주자를 베이스 위에, 인플레이 연출 중에는 대본대로 움직인다.
    /// 사람 모양은 선수 ID별로 재사용한다.
    /// </summary>
    public sealed class RunnersView
    {
        private const float BaseStandOffsetM = 0.8f;

        // 도루 지시를 건 주자는 리드를 더 크게
        private const float StealLeadM = 3f;

        private readonly Transform _parent;
        private readonly FieldGeometry _field;
        private readonly Dictionary<int, FigureView> _figures = new Dictionary<int, FigureView>();

        // 투구 사이 루상 주자 (인덱스 0 = 1루), 없으면 null
        private readonly FigureView[] _onBase = new FigureView[3];

        public RunnersView(Transform parent, FieldGeometry field)
        {
            _parent = parent;
            _field = field;
        }

        public FigureView Get(int playerId)
        {
            if (!_figures.TryGetValue(playerId, out FigureView figure))
            {
                figure = new FigureView(_parent, "Runner " + playerId, ProtoColors.Runner);
                _figures[playerId] = figure;
            }

            return figure;
        }

        public void HideAll()
        {
            foreach (FigureView figure in _figures.Values)
            {
                figure.Visible = false;
            }
        }

        /// <summary>현재 루상 주자를 베이스 옆(다음 베이스 쪽으로 조금 리드)에 세운다</summary>
        public void ShowBases(GameState state)
        {
            HideAll();
            for (int b = 0; b < 3; b++)
            {
                BaseRunner runner = state.Bases[b];
                _onBase[b] = null;
                if (runner == null)
                {
                    continue;
                }

                Vector3 basePoint = Point(b + 1);
                Vector3 lead = Vector3.MoveTowards(basePoint, Point(b + 2), BaseStandOffsetM);
                FigureView figure = Get(runner.PlayerId);
                figure.SetColor(ProtoColors.Runner);
                figure.Visible = true;
                figure.Snap(lead, Vector3.zero);
                _onBase[b] = figure;
            }
        }

        /// <summary>도루 지시를 건 주자 표시 (fromBase 1·2, 0이면 해제): 주황색, 리드 크게</summary>
        public void MarkSteal(int fromBase)
        {
            for (int b = 0; b < 3; b++)
            {
                FigureView figure = _onBase[b];
                if (figure == null)
                {
                    continue;
                }

                bool stealing = b + 1 == fromBase;
                figure.SetColor(stealing ? ProtoColors.RunnerSteal : ProtoColors.Runner);
                Vector3 lead = Vector3.MoveTowards(Point(b + 1), Point(b + 2), stealing ? StealLeadM : BaseStandOffsetM);
                figure.Snap(lead, Vector3.zero);
            }
        }

        private Vector3 Point(int baseIndex)
        {
            FieldPoint p = _field.Base(Mathf.Clamp(baseIndex, 0, 4));
            return new Vector3((float)p.X, 0f, (float)p.Y);
        }
    }
}
