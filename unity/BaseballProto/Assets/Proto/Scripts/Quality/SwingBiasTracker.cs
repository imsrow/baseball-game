using System.Collections.Generic;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 최근 스윙 N개의 평균 타이밍 오차·커서 상하 오차. 입력이 한쪽으로 쏠리는지 확인하는 디버그 표시용
    /// </summary>
    public sealed class SwingBiasTracker
    {
        private readonly Queue<BattingJudgement> _recent = new Queue<BattingJudgement>();
        private double _timingSumMs;
        private double _dzSum;

        public SwingBiasTracker(int capacity)
        {
            Capacity = capacity;
        }

        public int Capacity { get; }

        public int Count => _recent.Count;

        /// <summary>평균 타이밍 오차 (ms, − 이름 / + 늦음)</summary>
        public double MeanTimingErrorMs => Count > 0 ? _timingSumMs / Count : 0.0;

        /// <summary>평균 커서 − 공 높이 (m, + 커서가 위)</summary>
        public double MeanCursorDz => Count > 0 ? _dzSum / Count : 0.0;

        public void Clear()
        {
            _recent.Clear();
            _timingSumMs = 0;
            _dzSum = 0;
        }

        public void Add(BattingJudgement judgement)
        {
            _recent.Enqueue(judgement);
            _timingSumMs += judgement.TimingErrorMs;
            _dzSum += judgement.CursorDz;
            if (_recent.Count > Capacity)
            {
                BattingJudgement old = _recent.Dequeue();
                _timingSumMs -= old.TimingErrorMs;
                _dzSum -= old.CursorDz;
            }
        }
    }
}
