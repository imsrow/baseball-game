using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 구종별 값 표
    /// </summary>
    public sealed class PitchTypeTable
    {
        public PitchTypeTable()
        {
        }

        /// <summary>PitchType 순서(FF, SI, FC, SL, CU, CH, FS)의 7개 값</summary>
        public PitchTypeTable(params double[] values)
        {
            Values = values;
        }

        public double[] Values { get; set; } = new double[PitchTypeInfo.Count];

        public double Get(PitchType type)
        {
            return Values[(int)type];
        }
    }
}
