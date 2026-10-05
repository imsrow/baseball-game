using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Units;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 타구 비행 수치적분 (항력 + 백스핀 양력). 방향각은 비행 중 일정(사이드스핀 없음).
    /// </summary>
    public sealed class BallFlight
    {
        private readonly BallPhysicsConfig _physics;
        private readonly FieldGeometry _field;
        private readonly LeagueEnvironment _environment;

        public BallFlight(BallPhysicsConfig physics, FieldGeometry field, LeagueEnvironment environment)
        {
            _physics = physics;
            _field = field;
            _environment = environment;
        }

        /// <param name="liftMultiplier">양력 배율 (빗맞은 뜬공의 많은 백스핀 등)</param>
        /// <param name="carryMultiplier">타구별 비거리 편차 (항력을 이 값으로 나눔, 1 = 기본)</param>
        public FlightResult Simulate(double exitVelocityKmh, double launchAngleDeg, double sprayDeg,
            double liftMultiplier = 1.0, double carryMultiplier = 1.0)
        {
            BallPhysicsConfig p = _physics;
            double area = Math.PI * p.RadiusM * p.RadiusM;
            double dragK = 0.5 * p.AirDensity * p.DragCoefficient * area / p.MassKg / (_environment.CarryFactor * carryMultiplier);
            double liftCoefficient = launchAngleDeg > p.LiftStartDeg
                ? p.LiftCoefficientMax * Math.Min(1.0, (launchAngleDeg - p.LiftStartDeg) / (p.LiftRampDeg - p.LiftStartDeg))
                    * liftMultiplier
                : 0.0;
            double liftK = 0.5 * p.AirDensity * liftCoefficient * area / p.MassKg;

            double speed = UnitConversion.KmhToMetersPerSecond(exitVelocityKmh);
            double laRad = launchAngleDeg * Math.PI / 180.0;
            double vs = speed * Math.Cos(laRad);
            double vh = speed * Math.Sin(laRad);
            double s = 0;
            double h = p.ContactHeightM;
            double t = 0;
            double dt = p.TimeStepS;

            double fence = _field.FenceDistance(sprayDeg);
            var result = new FlightResult();
            bool catchSet = false;
            bool pastFence = false;
            double apex = h;

            while (t < p.MaxFlightTimeS)
            {
                double v = Math.Sqrt(vs * vs + vh * vh);
                double accS = -dragK * v * vs - liftK * v * vh;
                double accH = -dragK * v * vh + liftK * v * vs - p.Gravity;
                vs += accS * dt;
                vh += accH * dt;
                double prevS = s;
                double prevH = h;
                s += vs * dt;
                h += vh * dt;
                t += dt;
                apex = Math.Max(apex, h);

                // 펜스 통과 판정
                if (!pastFence && prevS < fence && s >= fence)
                {
                    double frac = (fence - prevS) / (s - prevS);
                    double heightAtFence = prevH + frac * (h - prevH);
                    double timeAtFence = t - dt + frac * dt;
                    if (heightAtFence > _field.Config.FenceHeightM)
                    {
                        pastFence = true;
                        result.IsHomeRun = true;
                    }
                    else
                    {
                        // 펜스에 맞고 떨어짐: 펜스 지점을 낙하 지점으로 처리
                        result.HitWall = true;
                        FieldPoint wall = FieldPoint.FromPolar(fence, sprayDeg);
                        result.LandingPoint = wall;
                        result.LandingTimeS = timeAtFence;
                        result.LandingHorizontalSpeedMps = 0;
                        if (!catchSet)
                        {
                            // 펜스 앞에서 뛰어올라 잡을 수 있는 높이
                            result.CatchPoint = wall;
                            result.CatchTimeS = timeAtFence;
                            result.CatchAtWall = true;
                        }

                        result.ApexM = apex;
                        result.DistanceM = fence;
                        return result;
                    }
                }

                // 하강 중 포구 높이 통과
                if (!catchSet && vh < 0 && prevH >= p.CatchHeightM && h < p.CatchHeightM)
                {
                    double frac = (prevH - p.CatchHeightM) / (prevH - h);
                    result.CatchPoint = FieldPoint.FromPolar(prevS + frac * (s - prevS), sprayDeg);
                    result.CatchTimeS = t - dt + frac * dt;
                    catchSet = true;
                }

                if (h <= 0)
                {
                    double frac = prevH / (prevH - h);
                    double landS = prevS + frac * (s - prevS);
                    result.LandingPoint = FieldPoint.FromPolar(landS, sprayDeg);
                    result.LandingTimeS = t - dt + frac * dt;
                    result.LandingHorizontalSpeedMps = vs;
                    if (!catchSet)
                    {
                        result.CatchPoint = result.LandingPoint;
                        result.CatchTimeS = result.LandingTimeS;
                    }

                    result.ApexM = apex;
                    result.DistanceM = landS;
                    return result;
                }
            }

            // 안전장치: 최대 시간 초과 시 마지막 위치를 낙하 지점으로
            result.LandingPoint = FieldPoint.FromPolar(s, sprayDeg);
            result.LandingTimeS = t;
            if (!catchSet)
            {
                result.CatchPoint = result.LandingPoint;
                result.CatchTimeS = t;
            }

            result.ApexM = apex;
            result.DistanceM = s;
            return result;
        }
    }
}
