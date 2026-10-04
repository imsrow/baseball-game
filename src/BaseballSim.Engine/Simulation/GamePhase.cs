namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 경기 진행 단계. 각 단계는 하나의 결정 지점이다.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>타석 시작: 공격 감독 결정 (대타·대주자, B단계)</summary>
        OffenseManager = 0,

        /// <summary>타석 시작: 수비 감독 결정 (투수 교체·대수비·고의4구)</summary>
        DefenseManager = 1,

        /// <summary>투수: 구종·목표 지점</summary>
        Pitch = 2,

        /// <summary>타자: 스윙 여부 (공은 이미 던져진 상태)</summary>
        Swing = 3,

        GameOver = 4,
    }
}
