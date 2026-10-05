using BaseballSim.Engine.Fielding;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 인플레이 연출용 경기장 장식 (큐브 수준): 내야 흙·잔디, 베이스, 파울 라인, 외야 펜스. 규격은 엔진 FieldConfig 그대로.
    /// 높이는 겹치는 판끼리 깜빡이지 않게 몇 mm씩 다르게 둔다.
    /// </summary>
    public sealed class StadiumView
    {
        private const float DirtSizeM = 33f;
        private const float InfieldGrassSizeM = 24f;
        private const float DirtTopM = 0.004f;
        private const float InfieldGrassTopM = 0.008f;
        private const float SlabThicknessM = 0.1f;
        private const float BaseSizeM = 0.45f;
        private const float BaseHeightM = 0.06f;
        private const float LineWidthM = 0.12f;
        private const float LineTopM = 0.012f;
        private const float WallThicknessM = 0.5f;
        private const float WallStepDeg = 5f;
        private const float FairAngleDeg = 45f;

        // 펜스 뒤 관중석: 뒤로 기운 판 (외야 잔디와 담장 밖을 구분)
        private const float StandsGapM = 1.5f;
        private const float StandsDepthM = 30f;
        private const float StandsTiltDeg = 60f;

        // 파울 라인은 타석 앞부터 (타석 시점에서 발밑 굵은 선으로 보이지 않게)
        private const float FoulLineStartM = 4f;

        public StadiumView(Transform root, FieldGeometry field)
        {
            float second = (float)field.Base(2).Y;
            Vector3 diamondCenter = new Vector3(0f, 0f, second * 0.5f);
            Slab(root, "InfieldDirt", diamondCenter, DirtSizeM, DirtTopM, ProtoColors.Dirt);
            Slab(root, "InfieldGrass", diamondCenter, InfieldGrassSizeM, InfieldGrassTopM, ProtoColors.InfieldGrass);

            for (int b = 1; b <= 3; b++)
            {
                FieldPoint p = field.Base(b);
                GameObject bag = PrimitiveFactory.Create(PrimitiveType.Cube, "Base" + b, root,
                    new Vector3((float)p.X, BaseHeightM * 0.5f, (float)p.Y), new Vector3(BaseSizeM, BaseHeightM, BaseSizeM),
                    ProtoColors.Chalk);
                bag.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }

            foreach (float side in new[] { -1f, 1f })
            {
                float angle = side * FairAngleDeg;
                float length = (float)field.FenceDistance(angle) - FoulLineStartM;
                Vector3 dir = Direction(angle);
                GameObject line = PrimitiveFactory.Create(PrimitiveType.Cube, "FoulLine", root,
                    dir * (FoulLineStartM + length * 0.5f) + Vector3.up * (LineTopM - 0.005f), new Vector3(LineWidthM, 0.01f, length),
                    ProtoColors.Chalk);
                line.transform.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            float height = (float)field.Config.FenceHeightM;
            for (float a = -FairAngleDeg; a < FairAngleDeg - 0.01f; a += WallStepDeg)
            {
                Vector3 p0 = Direction(a) * (float)field.FenceDistance(a);
                Vector3 p1 = Direction(a + WallStepDeg) * (float)field.FenceDistance(a + WallStepDeg);
                Vector3 along = p1 - p0;
                GameObject wall = PrimitiveFactory.Create(PrimitiveType.Cube, "Wall", root,
                    (p0 + p1) * 0.5f + Vector3.up * (height * 0.5f), new Vector3(WallThicknessM, height, along.magnitude + 0.3f),
                    ProtoColors.Wall);
                wall.transform.localRotation = Quaternion.LookRotation(along.normalized, Vector3.up);

                // 관중석: 담장 바로 뒤에서 바깥쪽으로 비스듬히 올라간다
                Vector3 outward = Vector3.Cross(Vector3.up, along).normalized;
                if (Vector3.Dot(outward, p0 + p1) < 0f)
                {
                    outward = -outward;
                }

                Quaternion tilt = Quaternion.LookRotation(along.normalized, Vector3.up)
                    * Quaternion.Euler(0f, 0f, Vector3.Dot(Vector3.Cross(along.normalized, Vector3.up), outward) > 0f ? StandsTiltDeg : -StandsTiltDeg);
                Vector3 up = tilt * Vector3.up;
                Vector3 baseCenter = (p0 + p1) * 0.5f + outward * StandsGapM;
                GameObject stands = PrimitiveFactory.Create(PrimitiveType.Cube, "Stands", root,
                    baseCenter + up * (StandsDepthM * 0.5f), new Vector3(WallThicknessM, StandsDepthM, along.magnitude * 1.3f),
                    ProtoColors.Stands);
                stands.transform.localRotation = tilt;
            }
        }

        /// <summary>방향각 (0 = 중견수, + = 1루 쪽) → 월드 수평 단위 벡터</summary>
        private static Vector3 Direction(float sprayDeg)
        {
            float rad = sprayDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>45도 돌린 정사각형 판 (다이아몬드). top이 윗면 높이</summary>
        private static void Slab(Transform root, string name, Vector3 center, float size, float top, Color color)
        {
            GameObject slab = PrimitiveFactory.Create(PrimitiveType.Cube, name, root,
                new Vector3(center.x, top - SlabThicknessM * 0.5f, center.z), new Vector3(size, SlabThicknessM, size), color);
            slab.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        }
    }
}
