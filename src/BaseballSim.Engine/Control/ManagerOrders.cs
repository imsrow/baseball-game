using System.Collections.Generic;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 결정: 지시 목록 (비어 있으면 아무것도 하지 않음)
    /// </summary>
    public sealed class ManagerOrders
    {
        public List<ManagerAction> Actions { get; set; } = new List<ManagerAction>();

        public bool IsEmpty => Actions.Count == 0;

        public static ManagerOrders None() => new ManagerOrders();

        public static ManagerOrders Of(params ManagerAction[] actions)
        {
            var orders = new ManagerOrders();
            orders.Actions.AddRange(actions);
            return orders;
        }
    }
}
