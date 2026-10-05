using System;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 프로토타입 조작·연출 계수 모음. 엔진 확률 계수는 여기 두지 않는다 (LeagueConfig.InputModifier).
    /// 인스펙터나 HUD 슬라이더로 실행 중 조정한다.
    /// </summary>
    [Serializable]
    public sealed class ProtoTuning
    {
        [Header("프레임")]
        public int TargetFrameRate = 60;

        [Header("카메라 (포수 뒤 시점)")]
        public float CameraHeightM = 1.0f;
        public float CameraBackM = 2.3f;
        public float CameraLookHeightM = 0.85f;

        /// <summary>가로 화면에서 바라보는 높이. 낮게 보면 존이 화면 세로 가운데로 올라와 하단 조작 영역과 겹치지 않는다</summary>
        public float CameraLookHeightLandscapeM = 0.1f;
        public float CameraLookAheadM = 8f;

        /// <summary>홈플레이트 위치에서 화면 가로에 들어올 폭 (m)</summary>
        public float PlateViewWidthM = 1.5f;

        /// <summary>홈플레이트 위치에서 화면 세로에 최소한 들어올 높이 (m). 가로 화면은 이 값에 맞춰 시야가 넓어진다</summary>
        public float PlateViewHeightM = 1.6f;

        public float MinVerticalFovDeg = 30f;

        [Header("투구 연출")]
        public float WindupS = 0.7f;

        /// <summary>투구 간격이 일정하지 않게 더하는 무작위 준비 시간 (연출 전용, 엔진 RNG 아님)</summary>
        public float WindupJitterS = 0.3f;

        public float ReleaseDistanceM = 16.8f;
        public float ReleaseHeightM = 1.75f;
        public float ReleaseSideM = 0.45f;

        /// <summary>공기저항으로 인한 실제 비행시간 증가 비율</summary>
        public float FlightDragFactor = 1.05f;

        /// <summary>실제 비행시간 대비 연출 배율 (1 = 실제 속도, 클수록 느림)</summary>
        public float FlightTimeScale = 1.3f;

        public float ArcHeightM = 0.12f;
        public float BreakScale = 1f;
        public float MittDepthM = 0.9f;
        public float BallVisualDiameterM = 0.1f;

        /// <summary>공 바로 아래 바닥 그림자 (깊이·타이밍 단서). FX 패널에서 켜고 끈다</summary>
        public bool BallShadow = true;

        public float BallShadowDiameterM = 0.12f;

        [Header("타격: 타이밍")]
        /// <summary>이 오차 이내면 타이밍 점수 1</summary>
        public float PerfectTimingMs = 25f;

        /// <summary>이 오차 이상이면 타이밍 점수 0</summary>
        public float ZeroTimingMs = 140f;

        /// <summary>이 오차에서 타이밍 방향(TimingDirection)이 ±1</summary>
        public float DirectionFullMs = 100f;

        /// <summary>
        /// 화면 표시 지연 보정 (ms). 공이 화면에 실제로 그려지는 시점이 계산보다 늦는 만큼 판정 기준을 미룬다.
        /// 기기마다 다르므로 HUD에서 맞춘다 (늘 늦게 판정되면 값을 올린다)
        /// </summary>
        public float DisplayLatencyMs = 0f;

        /// <summary>공 도달 후 이 시간까지 스윙 입력이 없으면 지켜봄(Take)</summary>
        public float LateCutoffMs = 120f;

        [Header("타격: 엔진 보정 상한 (Unity 기본값, 엔진 기본값 0.4와 별개)")]
        /// <summary>좋은 입력(TimingQuality +1)이 컨택 로그 오즈에 주는 최대 보상. 손맛을 위해 엔진 기본보다 크게</summary>
        public float SwingContactLogitCap = 1.0f;

        /// <summary>좋은 입력(TimingQuality +1)이 정타 로그 오즈에 주는 최대 보상</summary>
        public float SwingSolidLogitCap = 1.0f;

        /// <summary>나쁜 입력(TimingQuality −1)이 컨택 로그 오즈에서 빼는 최대 벌칙. 빗맞을 때 헛스윙이 너무 많지 않게 작게</summary>
        public float SwingContactLogitPenalty = 0.4f;

        /// <summary>나쁜 입력(TimingQuality −1)이 정타 로그 오즈에서 빼는 최대 벌칙</summary>
        public float SwingSolidLogitPenalty = 0.4f;

        [Header("타격: 품질 합산")]
        /// <summary>TimingQuality 가중 평균에서 타이밍 점수 비중 (커서 점수 비중 = 1 − 이 값)</summary>
        public float TimingWeight = 0.6f;

        [Header("타격: 커서")]
        public float CursorRadiusM = 0.12f;

        /// <summary>커서 중심 거리/반지름이 이 이하면 커서 점수 1</summary>
        public float CursorPerfectRatio = 0.3f;

        /// <summary>커서 중심 거리/반지름이 이 이상이면 커서 점수 0</summary>
        public float CursorZeroRatio = 1.2f;

        /// <summary>드래그 감도 (화면 이동량 대비 커서 이동량 배율)</summary>
        public float DragSensitivity = 1.2f;

        public float CursorLimitXM = 0.5f;
        public float CursorMinZM = 0.25f;
        public float CursorMaxZM = 1.3f;

        /// <summary>홀드 모드: 커서 중심이 스트라이크 존 테두리에서 이만큼 더 밖에 있을 때 손을 떼면 스윙 취소(지켜봄)</summary>
        public float HoldTakeMarginM = 0.10f;

        [Header("타격: 디버그")]
        /// <summary>HUD 평균 타이밍·커서 오차를 낼 최근 스윙 개수 (쏠림 확인용)</summary>
        public int SwingBiasWindow = 10;

        [Header("타격: 스윙 연출")]
        public float SwingDurationS = 0.18f;

        [Header("투구: 게이지")]
        /// <summary>게이지 0→1→0 왕복 주기</summary>
        public float GaugePeriodS = 1.1f;

        public float GaugeSweetCenter = 0.85f;

        /// <summary>스위트 중심과의 차이가 이 이하면 릴리스 점수 1</summary>
        public float GaugePerfectBand = 0.03f;

        /// <summary>스위트 중심과의 차이가 이 이상이면 릴리스 점수 0</summary>
        public float GaugeZeroBand = 0.45f;

        /// <summary>게이지를 멈추지 않으면 이 시간 후 최저 품질로 투구</summary>
        public float GaugeTimeoutS = 5f;

        public float TargetLimitXM = 0.5f;
        public float TargetMinZM = 0.15f;
        public float TargetMaxZM = 1.4f;

        [Header("결과 연출")]
        public float ResultHoldS = 1.1f;
        public float MaxBattedBallDisplayS = 3f;
        public float GroundBallSpeedRatio = 0.6f;
        public float MaxGroundBallDisplayS = 2f;
        public float FoulDisplayS = 1.2f;

        public ProtoTuning Clone()
        {
            return (ProtoTuning)MemberwiseClone();
        }
    }
}
