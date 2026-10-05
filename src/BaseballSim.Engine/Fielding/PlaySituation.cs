namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 인플레이 판정 입력: 아웃 카운트, 주자, 타자주자, 수비 배치
    /// </summary>
    public sealed class PlaySituation
    {
        public int OutsBefore { get; set; }

        /// <summary>인덱스 0 = 1루, 1 = 2루, 2 = 3루. 비어 있으면 null</summary>
        public RunnerProfile[] Runners { get; set; } = new RunnerProfile[3];

        public RunnerProfile Batter { get; set; }

        public DefensiveAlignment Defense { get; set; }

        /// <summary>공격 팀 주루 성향 (추가 진루·태그업 판단)</summary>
        public Control.BaserunningStyle BaserunningStyle { get; set; }

        /// <summary>base(1~3)에 주자가 있으면 반환</summary>
        public RunnerProfile RunnerOn(int baseNumber)
        {
            return Runners[baseNumber - 1];
        }

        /// <summary>타자가 1루로 가면 base(1~3)의 주자가 밀려나는지 (포스 상태)</summary>
        public bool IsForced(int baseNumber)
        {
            for (int b = 1; b <= baseNumber; b++)
            {
                if (Runners[b - 1] == null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
