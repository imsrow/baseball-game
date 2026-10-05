namespace BaseballProto.Core
{
    /// <summary>연출 이동 구간의 속도 변화</summary>
    public enum MotionEase
    {
        /// <summary>일정한 속도 (날아가는 공)</summary>
        Linear = 0,

        /// <summary>점점 느려짐 (굴러가다 서는 공)</summary>
        EaseOut = 1,

        /// <summary>처음 30%는 가속, 그 뒤 일정 (달리기 시작하는 사람)</summary>
        Accelerate = 2,

        /// <summary>천천히 출발해 천천히 멈춤 (자리 잡기)</summary>
        Smooth = 3,
    }
}
