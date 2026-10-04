using System.Collections.Generic;
using BaseballSim.Engine.AI.ManagerAI;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 사람 감독.
    /// 작전(도루·번트·고의4구)과 교체는 UI 버튼으로 미리 걸어두면(Queue) 해당 결정 지점에 적용되고,
    /// 걸어둔 것이 없으면 "작전 없음"으로 바로 진행한다 (멈추지 않는다).
    /// 단, AI라면 교체할 의미 있는 순간(투수 피로 임계치, 대타·대수비 기회)에는 추천과 함께 입력을 기다린다.
    /// DelegateToAi를 켜면 작전·교체를 모두 AI에게 맡긴다.
    /// </summary>
    public sealed class HumanManagerDecision : IManagerDecision
    {
        private readonly ManagerAi _ai = new ManagerAi();
        private readonly List<ManagerAction> _queued = new List<ManagerAction>();
        private int _declinedPitcherId = -1;
        private int _declinedAtBattersFaced;

        /// <summary>작전·교체를 AI에게 맡김</summary>
        public bool DelegateToAi { get; set; }

        /// <summary>미리 걸어둔 지시 (적용되거나 무효가 되면 제거)</summary>
        public IReadOnlyList<ManagerAction> Queued => _queued;

        /// <summary>버튼 입력: 다음 해당 결정 지점에 적용할 지시를 걸어둔다</summary>
        public void Queue(ManagerAction action)
        {
            _queued.RemoveAll(a => a.Type == action.Type);
            _queued.Add(action);
        }

        public void ClearQueue()
        {
            _queued.Clear();
        }

        public Decision<ManagerOrders> DecidePrePitch(ManagerContext context)
        {
            if (DelegateToAi)
            {
                return _ai.DecidePrePitch(context);
            }

            return Decision<ManagerOrders>.Ready(TakeQueued(ManagerActionType.StealAttempt, ManagerActionType.BuntSign));
        }

        public Decision<ManagerOrders> DecideOffense(ManagerContext context)
        {
            if (DelegateToAi)
            {
                return _ai.DecideOffense(context);
            }

            ManagerOrders queued = TakeQueued(ManagerActionType.PinchHitter, ManagerActionType.PinchRunner);
            if (!queued.IsEmpty)
            {
                return Decision<ManagerOrders>.Ready(queued);
            }

            // AI라면 교체할 순간에만 확인
            ManagerOrders suggestion = _ai.DecideOffense(context).Value;
            return Prompt(context, suggestion);
        }

        public Decision<ManagerOrders> DecideDefense(ManagerContext context)
        {
            if (DelegateToAi)
            {
                return _ai.DecideDefense(context);
            }

            ManagerOrders queued = TakeQueued(ManagerActionType.PitchingChange, ManagerActionType.DefensiveSubstitution,
                ManagerActionType.IntentionalWalk);
            if (!queued.IsEmpty)
            {
                return Decision<ManagerOrders>.Ready(queued);
            }

            var suggestion = new ManagerOrders();
            suggestion.Actions.AddRange(SubstitutionAdvisor.DefensiveSubs(context));
            int reliever = PitchingChangeAdvisor.Advise(context);
            if (reliever >= 0 && !RecentlyDeclined(context))
            {
                suggestion.Actions.Add(ManagerAction.PitchingChange(reliever));
            }

            return Prompt(context, suggestion);
        }

        /// <summary>
        /// 확인 요청 중 사람이 "교체 안 함"을 고르면 엔진이 알려준다. 같은 투수는 몇 타자 뒤에 다시 묻는다.
        /// </summary>
        public void OnSuggestionDeclined(ManagerContext context)
        {
            if (context.Suggestion == null)
            {
                return;
            }

            foreach (ManagerAction action in context.Suggestion.Actions)
            {
                if (action.Type == ManagerActionType.PitchingChange)
                {
                    PitcherGameState current = context.Team.CurrentPitcher;
                    _declinedPitcherId = current.PlayerId;
                    _declinedAtBattersFaced = current.BattersFaced;
                }
            }
        }

        private bool RecentlyDeclined(ManagerContext context)
        {
            PitcherGameState current = context.Team.CurrentPitcher;
            return current.PlayerId == _declinedPitcherId
                && current.BattersFaced - _declinedAtBattersFaced < context.Config.Interaction.DeclinedChangeRepromptBatters;
        }

        private static Decision<ManagerOrders> Prompt(ManagerContext context, ManagerOrders suggestion)
        {
            if (suggestion.IsEmpty)
            {
                return Decision<ManagerOrders>.Ready(ManagerOrders.None());
            }

            context.Suggestion = suggestion;
            return Decision<ManagerOrders>.Pending;
        }

        /// <summary>걸어둔 지시 중 해당 종류를 꺼낸다 (유효성은 엔진이 다시 검증하고, 무효면 작전 없음으로 진행)</summary>
        private ManagerOrders TakeQueued(params ManagerActionType[] types)
        {
            var orders = new ManagerOrders();
            for (int i = _queued.Count - 1; i >= 0; i--)
            {
                if (System.Array.IndexOf(types, _queued[i].Type) >= 0)
                {
                    orders.Actions.Insert(0, _queued[i]);
                    _queued.RemoveAt(i);
                }
            }

            return orders;
        }
    }
}
