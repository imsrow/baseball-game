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
        /// 기기마다 다르므로 HUD에서 맞춘다 (늘 늦게 판정되면 값을 올린다). 개인 습관(늘 이르게 침)도 음수로 여기서 맞춘다.
        /// TUNE의 타이밍 보정(TimingCalibration)이 자동으로 정하고 기기에 저장한다
        /// </summary>
        public float DisplayLatencyMs = 0f;

        /// <summary>타이밍 보정: 이 스윙 수의 평균 오차로 Calib ms를 정한다</summary>
        public int CalibSwingCount = 10;

        /// <summary>타이밍 보정: 오차가 이보다 큰 스윙은 측정에서 뺀다 (체크 스윙·엉뚱한 입력)</summary>
        public float CalibMaxAbsErrorMs = 300f;

        /// <summary>타이밍 보정 결과 안내 표시 시간</summary>
        public float CalibResultShowS = 4f;

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
        public float CursorZeroRatio = 2.0f;

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

        [Header("자동 진행 (연출·프레임 확인용)")]
        /// <summary>사람 몫의 스윙·투구도 AI가 정하고 다음 공을 자동으로 시작한다. FX 패널에서 켜고 끈다</summary>
        public bool AutoPlay;

        /// <summary>자동 진행 때 다음 공까지 쉬는 시간</summary>
        public float AutoPlayPauseS = 0.6f;

        [Header("인플레이 연출 (엔진 결과를 보여주기만 함)")]
        /// <summary>타구 카메라: 인플레이 타구를 따라가는 시점. FX 패널에서 켜고 끈다</summary>
        public bool BallCam = true;

        /// <summary>인플레이 연출 재생 배속 (FX 패널: 1x / 1.5x / 2x)</summary>
        public float PlaySpeed = 1f;

        /// <summary>연출이 이보다 길면 더 빨리 재생해 이 시간 안에 끝낸다 (화면 시간, s)</summary>
        public float MaxPlayDisplayS = 7f;

        /// <summary>외야수 첫 발 시각. 엔진 반응 시간(첫 발 지연 + 경로 손실)과 달리 바로 움직이고, 곡선 경로·속도로 도착 시각을 맞춘다</summary>
        public float OutfielderFirstStepS = 0.3f;

        public float InfielderFirstStepS = 0.2f;

        /// <summary>뜬공을 여유 있게 잡을 때 낙하 지점에 미리 도착해 자리 잡는 시간</summary>
        public float CatchSettleS = 0.5f;

        /// <summary>연출용 야수 최고 이동 속도 (m/s). 엔진 도착 시각이 이보다 빠른 속도를 요구하면 늦게 도착</summary>
        public float FielderMaxSpeedMps = 9f;

        /// <summary>곡선 경로 휨 (이동 거리 대비): 외야수 / 내야수</summary>
        public float OutfielderRouteBend = 0.15f;

        public float InfielderRouteBend = 0.05f;

        /// <summary>송구가 주자보다 먼저(아웃)·나중(세이프) 도착하는 간격</summary>
        public float OutSyncMarginS = 0.15f;

        /// <summary>주자 속도 조정 범위: 그 주자 스피드 능력치 ± 이 표준편차(1 SD = 10점)에 해당하는 시간 안에서만</summary>
        public float RunnerSpeedRangeSd = 1.5f;

        /// <summary>송구 속도 조정 범위 (엔진 송구 속도 대비 배율)</summary>
        public float ThrowSpeedMinRatio = 0.7f;

        public float ThrowSpeedMaxRatio = 1.3f;

        /// <summary>송구 준비 시간 하한 (공을 잡은 뒤, s)</summary>
        public float MinThrowTransferS = 0.3f;

        public float ThrowReleaseHeightM = 1.8f;
        public float GloveHeightM = 1.2f;

        /// <summary>송구 궤적 높이 (송구 거리 대비)</summary>
        public float ThrowArcRatio = 0.05f;

        /// <summary>땅볼 바운드 수·첫 바운드 높이</summary>
        public int GroundBallHops = 3;

        public float GroundBallHopHeightM = 0.7f;

        /// <summary>외야에 떨어진 공의 첫 바운드 높이</summary>
        public float LandingHopHeightM = 1.2f;

        /// <summary>악송구가 베이스를 지나쳐 가는 거리</summary>
        public float OvershootM = 12f;

        /// <summary>뜬공 때 주자가 떨어졌다 돌아오는 리드 거리</summary>
        public float RunnerLeadM = 3f;

        /// <summary>뜬공 아웃 때 타자가 1루 쪽으로 뛰는 속도 배율 (클수록 느림, 조깅)</summary>
        public float BatterJogScale = 1.3f;

        /// <summary>홈런 베이스 일주 속도 배율 (클수록 느림)</summary>
        public float HomeRunTrotScale = 1.7f;

        /// <summary>아웃·득점한 주자가 사라지기까지</summary>
        public float RunnerHideDelayS = 0.7f;

        /// <summary>연출 대상이 아닌 야수가 공 쪽으로 움직이는 거리 (최대, m)</summary>
        public float DriftMaxM = 4f;

        /// <summary>옆 외야수가 공 쪽으로 백업 가는 비율 (거리 대비, 최대 BackupMaxM)</summary>
        public float BackupRatio = 0.35f;

        public float BackupMaxM = 18f;

        /// <summary>마지막 동작 뒤 연출 여유 / 홈런은 공이 떨어진 뒤 여유</summary>
        public float PlayEndPadS = 0.5f;

        public float HomeRunTailS = 1.5f;

        [Header("타구 카메라")]
        public float BallCamBlendS = 0.4f;

        /// <summary>
        /// 카메라 위치 (높이, 관심 지점에서 홈 쪽으로 물러난 거리): 멀리(홈런·장타) / 중간 / 가까이(땅볼).
        /// 홈에서 관심 지점을 바라보는 방향 뒤에서 따라간다
        /// </summary>
        public Vector2 BallCamFar = new Vector2(36f, 40f);

        public Vector2 BallCamMid = new Vector2(26f, 28f);
        public Vector2 BallCamNear = new Vector2(17f, 17f);

        /// <summary>카메라가 바라보는 높이에 공 높이를 얼마나 반영할지 (작을수록 땅을 내려다본다)</summary>
        public float BallCamLookHeightRatio = 0.3f;

        public float BallCamMoveSmoothS = 0.45f;

        /// <summary>타구 카메라에서 공 크기 배율 (실제 크기는 수십 m 밖에서 안 보인다)</summary>
        public float BallCamBallScale = 4f;

        /// <summary>장타 시점을 쓰는 비거리 (m)</summary>
        public float BallCamFarDistanceM = 80f;

        public float BallCamMinFovDeg = 34f;
        public float BallCamMaxFovDeg = 72f;

        /// <summary>관심 지점이 화면 가장자리에 붙지 않게 시야 여유 배율</summary>
        public float BallCamFitMargin = 1.25f;

        public float BallCamLookSmoothS = 0.25f;
        public float BallCamFovSmoothS = 0.35f;

        public ProtoTuning Clone()
        {
            return (ProtoTuning)MemberwiseClone();
        }
    }
}
