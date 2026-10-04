namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 카운트(볼 0~3 × 스트라이크 0~2)별 값 표
    /// </summary>
    public sealed class CountTable
    {
        public const int BallStates = 4;
        public const int StrikeStates = 3;

        public CountTable()
        {
        }

        /// <summary>
        /// 볼 0개 행부터 차례로 [0-0, 0-1, 0-2, 1-0, 1-1, 1-2, 2-0, ... 3-2] 순서의 12개 값
        /// </summary>
        public CountTable(params double[] values)
        {
            Values = values;
        }

        public double[] Values { get; set; } = new double[BallStates * StrikeStates];

        public double Get(int balls, int strikes)
        {
            return Values[balls * StrikeStates + strikes];
        }

        public static CountTable Uniform(double value)
        {
            var table = new CountTable();
            for (int i = 0; i < table.Values.Length; i++)
            {
                table.Values[i] = value;
            }

            return table;
        }
    }
}
