namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 구종 7종. 값은 배열 인덱스로 쓰인다.
    /// </summary>
    public enum PitchType
    {
        /// <summary>포심 패스트볼</summary>
        FourSeam = 0,

        /// <summary>싱커(투심)</summary>
        Sinker = 1,

        /// <summary>커터</summary>
        Cutter = 2,

        /// <summary>슬라이더</summary>
        Slider = 3,

        /// <summary>커브</summary>
        Curveball = 4,

        /// <summary>체인지업</summary>
        Changeup = 5,

        /// <summary>스플리터</summary>
        Splitter = 6,
    }
}
