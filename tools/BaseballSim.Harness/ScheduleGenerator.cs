using System;
using System.Collections.Generic;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 라운드로빈 시리즈 일정. 각 팀 쌍이 상대팀별 경기 수만큼, 시리즈 단위로 홈/원정을 번갈아 만난다.
    /// </summary>
    public static class ScheduleGenerator
    {
        /// <summary>날짜별 경기 목록 (홈 팀 인덱스, 원정 팀 인덱스)</summary>
        public static List<List<(int Home, int Away)>> Generate(int teamCount, int gamesPerOpponent, int seriesLength)
        {
            // 홀수 팀이면 휴식(−1) 슬롯 추가
            int n = teamCount % 2 == 0 ? teamCount : teamCount + 1;
            var slots = new List<int>();
            for (int i = 0; i < n; i++)
            {
                slots.Add(i < teamCount ? i : -1);
            }

            int seriesCount = (gamesPerOpponent + seriesLength - 1) / seriesLength;
            var days = new List<List<(int, int)>>();
            for (int cycle = 0; cycle < seriesCount; cycle++)
            {
                int length = Math.Min(seriesLength, gamesPerOpponent - cycle * seriesLength);
                var rotation = new List<int>(slots);
                for (int round = 0; round < n - 1; round++)
                {
                    var pairs = new List<(int, int)>();
                    for (int i = 0; i < n / 2; i++)
                    {
                        int a = rotation[i];
                        int b = rotation[n - 1 - i];
                        if (a < 0 || b < 0)
                        {
                            continue;
                        }

                        bool swap = ((round + i) % 2 == 1) ^ (cycle % 2 == 1);
                        pairs.Add(swap ? (b, a) : (a, b));
                    }

                    for (int g = 0; g < length; g++)
                    {
                        days.Add(pairs);
                    }

                    // 원형 회전 (첫 슬롯 고정)
                    int last = rotation[n - 1];
                    rotation.RemoveAt(n - 1);
                    rotation.Insert(1, last);
                }
            }

            return days;
        }
    }
}
