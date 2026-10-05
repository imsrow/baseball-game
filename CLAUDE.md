# CLAUDE.md

## 프로젝트 개요

모바일(iOS/Android) 야구 게임을 Unity(C#)로 개발 중이다.

### 목표

- 프로야구9처럼 가볍게 즐기는 모바일 야구
- 직접 투구·타격이 가능한 손맛 좋은 조작
- 확장된 세이버메트릭스 스탯 (기록 화면이 깊이를 담당)
- 시뮬레이션 현실성은 "그럴듯한 수준"이면 충분하다. OOTP급 정밀 시뮬레이션은 목표가 아니다.

## 구조 원칙

1. **시뮬레이션 엔진은 Unity 의존성 없는 순수 C# 라이브러리** (.NET Standard 2.1, C# 9)
2. **조작 레이어는 엔진 확률을 '보정'만 한다.** 결과는 항상 엔진이 결정
3. **모든 투구를 이벤트 로그로 저장**하고, 기록은 로그에서 파생
4. **난수는 시드 고정 가능한 RNG를 주입**받아 재현 가능하게

## 시뮬레이션 방향

- 유지하는 핵심: 능력치 → 확률(log5 / odds ratio), 투구 단위 진행, 상태 머신(Step/Submit),
  직접 플레이 ↔ 시뮬 전환, 이벤트 로그
- 단순한 모델을 우선한다. 튜닝이 잘 맞지 않는 세부 물리(수비 범위, 뜬공 처리 등)는
  확률표 + 능력치 보정으로 대체한다.
- 이미 잘 동작하는 코드는 뜯어고치지 않는다. 변경 범위는 최소로.
- 타구속도·발사각 등 Statcast식 수치는 통계 표시용으로 계속 생성한다.
- 작전·주루 규칙(도루, 번트, 교체 등)도 단순 확률 모델로 구현한다.

## 튜닝 기준

- 합격 기준: 리그 AVG, OBP, SLG, K%, BB%, HR%가 목표치에 대략 맞으면 된다.
  허용 오차는 LeagueTargets의 2배 수준 (AVG·OBP ±.010, SLG ±.020, K%·BB% ±1.0%p, HR% ±0.6%p).
- 세부 지표(구역별 스윙률, 타구 유형별 BABIP 등)는 참고만 하고 맞추지 않는다.
- 튜닝 계수는 모두 LeagueConfig에, 리그 성향·목표치는 LeagueEnvironment 프리셋에 둔다.
- 검증 하네스는 시드 1개로, 사용자가 요청할 때만 실행한다.

## 콘텐츠 정책

- 가상 리그 사용 (실제 선수·구단명 사용 안 함)

## 코드 스타일

- 클래스별 파일 분리
- 주석은 한국어
- 매직넘버는 설정 클래스로 분리

## 빌드·테스트

- `dotnet build`, `dotnet test` (xUnit). 테스트는 엔진(`tests/BaseballSim.Engine.Tests`)과 인플레이 연출 대본(`tests/BaseballProto.PlayScript.Tests`:
  Unity `Assets/Proto/Scripts/Core`의 대본 코드를 그대로 컴파일하고 UnityEngine은 `UnityEngineStub.cs`로 흉내. 대본 쪽 Unity 스크립트에
  새 UnityEngine 타입을 쓰면 스텁에도 추가) 이 PC에는 .NET 10 런타임만 있어 테스트·도구 실행 시 `DOTNET_ROLL_FORWARD=Major` 필요
- 하네스 (요청 시에만): `dotnet run -c Release --project tools/BaseballSim.Harness -- --seeds 1`
- 밸런스 비교 (사람처럼 던지기·치기 vs AI, 구역별 성적): `dotnet run -c Release --project tools/BaseballSim.BalanceProbe -- --games 2000 --scenarios a,b,f,g,h,i [--swing-caps 1.0] [--swing-penalty 0.4] [--ev-la]`
  (`--ev-la`: 타구속도 × 발사각 구간별 타율·장타율 표, 강한 라이너 아웃의 비거리·체공·포구 수비수.
  `--hit-types`: 낙하 거리 구간별 1·2·3루타 비율, 짧은 장타 예시)
- WebGL 빌드 (에디터를 닫고): `Unity.exe -batchmode -projectPath unity/BaseballProto -buildTarget WebGL -executeMethod BaseballProto.EditorTools.ProtoBuild.BuildWebGLBatch`
  (에디터 메뉴 Baseball > Build WebGL도 같음). 결과물 `unity/BaseballProto/Builds/WebGL`
- WebGL 배포: `powershell -ExecutionPolicy Bypass -File tools/deploy-webgl.ps1` → gh-pages 브랜치 → https://imsrow.github.io/baseball-game/
  - Pages는 Content-Encoding 헤더를 못 붙여 Gzip + Decompression Fallback(JS 해제), 파일명 해시
  - 템플릿 `Assets/WebGLTemplates/BaseballPWA`: iOS 홈 화면 메타, 네트워크 우선 서비스 워커, 렌더 배율 상한 2
  - WebGL은 첫 탭 전 소리가 안 나므로 TAP TO START 후 시작. targetFrameRate는 −1(브라우저 rAF)
- 인플레이 연출 스냅샷 (에디터를 닫고): `Unity.exe -batchmode -quit -projectPath unity/BaseballProto -executeMethod BaseballProto.EditorTools.PlaySnapshots.RenderBatch`
  (에디터 메뉴 Baseball > Render Play Snapshots). AI 경기에서 홈런·2루타·뜬공·병살·땅볼·안타 실제 이벤트를 골라 연출을 재생하며
  6장씩 찍어 `plays_portrait.png`·`plays_landscape.png`로 저장. 출력 폴더는 환경 변수 `PLAY_SNAPSHOT_DIR` (없으면 `Builds/Snapshots`)
- Unity 엔진 DLL 갱신: `powershell -ExecutionPolicy Bypass -File tools/sync-engine.ps1` (또는 에디터 메뉴 Baseball > Sync Engine DLL).
  엔진을 고치면 반드시 다시 실행. DLL은 git에 넣지 않는다

## Git

- 작업은 main 브랜치에 커밋하고 푸시한다
- WebGL 빌드 결과물은 main에 넣지 않고 gh-pages 브랜치에만 올린다 (GitHub Pages)

## 현재 상태

- 엔진 1단계 완료 (마지막 엔진 커밋 `b37c208`)
  - A: 상태 머신, 투구 판정(실행·인지·스윙·컨택), 타구 생성·비행, 물리 기반 수비·주루, 기본 AI, 검증 하네스
  - B: 도루·번트·폭투·포일·낫아웃, 고의4구, 대타·대주자·대수비, 불펜 역할(마무리·셋업)
  - C: 직접↔시뮬 전환, 멈춤 조건, 사람 감독 흐름, 저장/불러오기
- 하네스(시드 1): 6개 목표 지표 모두 허용 범위 내. xUnit 테스트 111개 통과 (엔진 106, 연출 대본 5)
- 타구 판정 조정 (2026-10-05, `--ev-la` 기준): 강한 라이너(161+, 10~25도) 아웃 31% → 21%, 약한 뜬공 역전 축소,
  강한 고각 타구에 펜스 앞 아웃. 방법:
  - 양력은 `BallPhysicsConfig.LiftStartDeg`(15도)부터 증가 → 라이너가 낮고 짧게 날아감
  - 빗맞은 타구(`IsSolid` 아님)는 백스핀이 많아 양력 × `WeakContactLiftMultiplier`(2.5) → 높이 떠서 잡힘
  - 타구별 비거리 편차: 항력 배율 `CarryNoiseSd`(0.06, 0.85~1.15) → 경계 타구가 펜스 앞에서 잡히기도 함
  - 내야수 뜬공 추격 속도 `FieldingConfig.InfieldAirBallSpeedMps`(6.5, 땅볼 횡이동 4.2와 별도)
  - 펜스에 맞을 타구도 포구 높이 위면 펜스 앞 포구 시도 (`FlightResult.CatchAtWall`, 추가 시간 `WallCatchExtraS` 0.5)
  - 라인드라이브 외야 추가 반응 `OutfieldLineDriveExtraReactionS` 0.3 → 0.35
- 안타 종류 판정 수정 (2026-10-05, `BalanceProbe --hit-types`): 60 m 미만에 떨어진 안타의 2루타 39% → 8%
  - 원인: 외야 회수가 내야 땅볼식 "정면 처리"라, 외야수 쪽으로 굴러오는 공을 제자리에서 기다렸다 (회수 1~2초 지연)
  - 외야 회수는 달려 나와 가장 이르게 잡는 지점 (`FindEarliestIntercept`). 멀어지는 공을 쫓아 잡으면 추가 시간
    `OutfieldChasePickupExtraS`(1.1, 정면 0·옆 절반·뒤 전부)
  - 잡힐 것 같던 뜬공이 떨어지면 타자가 늦음: `BaserunningConfig.BatterRoutineFlyDelayS`(1.0) × 포구 확률
  - 외야: 반응 `OutfieldReactionS` 1.0(오전에 1.2로 올렸던 것 되돌림), 평균 속도 `OutfieldSpeedMps` 7.9 → 7.2
    (가속 포함 평균이라 최고 속도 8.2보다 충분히 낮게. 갭 커버 범위가 줄어 깊은 안타가 늘어남)
- 타구 연출용 시각: `BattedBallData.HasLanding/LandingX/LandingY/LandingTimeS/FieldedTimeS/FielderArrivalS`
  (판정·난수 무관, 저장 형식 v2, v1도 읽음)
- 투구 위치 효과: 구역(Heart/Shadow/Chase/Waste)별 정타 로그 오즈·타구속도 보정 (`BattedBallConfig.SolidLogitShiftByRegion`,
  `ExitVelocityKmhByRegion`), 전체 배율 `LeagueConfig.LocationEffectScale`(기본 1 = Statcast 근사, Unity TUNE에서 조절).
  Standard 프리셋 구역별 컨택률은 Heart 헛스윙 약 13%, Chase 약 47%로 조정. 목표: Heart 타율 .300·장타율 .550 근처, Chase 타율 .150 근처
- 2단계 완료 (2026-10-05): Unity 1:1 투타 대결 프로토타입 (`unity/BaseballProto`, Unity 6000.6.4f1, URP, 세로/가로 자동 전환).
  WebGL로 아이폰 홈 화면 앱 배포 중. 마지막 2단계 커밋은 git log 참고
  - 엔진 최소 확장: `BatterAction.TimingDirection`(이르면 당겨치기), `CursorVerticalOffset`(커서가 위면 발사각 낮게),
    상한은 `InputModifierConfig.MaxTimingSprayShiftDeg` / `MaxCursorLaunchAngleShiftDeg`. 타격 보정 상한은 보상(+)·벌칙(−) 분리.
    AI는 null(중립)이라 하네스 결과 불변
  - 타격 조작 3종: 홀드(세로 기본) / 드래그 패드 + 스윙 버튼(가로 기본) / 탭. 판정은 입력 이벤트 타임스탬프 기준
  - 홀드 조작: 커서를 존 밖(테두리 + `ProtoTuning.HoldTakeMarginM`)에서 떼면 스윙 취소(회색 커서) = 볼을 참는 방법
  - 개인 타이밍 보정: TUNE의 TIMING CALIB → 다음 10스윙의 보정 전 평균 오차를 Calib ms(`DisplayLatencyMs`)에 넣고
    PlayerPrefs(`TimingCalibMs`)에 저장. 기준 시각만 옮기므로 구종별 도달 시각 차이는 그대로. TUNE RESET에도 유지, CALIB = 0으로 초기화
  - 투구 연출: 휨은 u²(중력처럼 점점), 공 바로 아래 바닥 그림자(FX에서 끄기). 포수 뒤 근접 카메라라 공이 마지막 약 50ms에
    화면상 크게 떨어져 보이고, 그 전엔 존 위쪽에 떠 보여 이르게·높게 치는 쏠림이 생긴다 (디버그의 최근 10스윙 평균으로 확인)
  - 투구 조작: 구종 버튼 → 존 탭으로 목표 → 게이지 멈춤(릴리스 품질)
  - 디버그 표시: 입력 오차 → 엔진 보정량 → 결과, 최근 10스윙 평균 타이밍·dz, 현재 calib. FX: 진동·소리·히트스톱·흔들림·그림자·커서 로그

- 3단계 (2026-10-05): 인플레이 타구 연출. 결과는 엔진이 정하고 연출은 보여주기만 한다
  - 흐름: `PlayScriptBuilder`(엔진 PitchEvent → 대본 `PlayScript`: 공·야수·주자 `MotionTrack`, 송구) → `PlayDirector`(재생·건너뛰기)
    → `FieldersView`·`RunnersView`·`BallView`·`BallCamera`. 경기장 장식은 `StadiumView`(내야, 베이스, 파울 라인, 펜스, 관중석)
  - 엔진 표시용 필드(`BattedBallData.LandingX/Y/LandingTimeS/FieldedTimeS/FielderArrivalS`)를 그대로 지난다: 공은 엔진 낙하·포구 지점과 시각,
    처리 야수는 0.3초(내야 0.2초)에 첫 발 → 곡선 경로·가속으로 엔진 도착 시각에 도착. 여유 있는 뜬공은 조깅해서 자리 잡고 들어오며 포구,
    엔진이 늦게 도착해도 잡았으면 몸을 날림. 송구 받는 야수는 베이스 커버, 나머지는 공 쪽으로 반응(옆 외야수는 백업)
  - 아웃·세이프 순서: 주자 속도는 그 주자 스피드 ±1.5 SD 시간 안에서만 맞추고, 모자라면 송구 속도(0.7~1.3배)·시작 시각을 조정
  - 타구 카메라: 관심 지점(공, 공 아래 땅, 처리 야수 또는 송구 받는 곳)을 홈 쪽 뒤·위에서 따라감. 홈런·장타 멀리, 땅볼 가까이.
    시야각은 화면 비율로 맞춤(세로·가로). 공은 카메라가 멀어지는 만큼 키움(`BallCamBallScale`). 포수 모양은 타석 시점 카메라 바로 앞이라
    카메라가 넘어간 뒤에만 보임
  - FX 패널: Ball cam 켜고 끄기, Play speed 1x/1.5x/2x(연출이 7초 넘으면 더 빨리), Auto play(사람 몫 스윙·투구도 AI, 다음 공 자동: 연출·프레임 확인용)
  - 연출 중 탭(또는 스페이스) = 결과로 바로. 결과 문구는 연출이 끝난 뒤
  - 이닝을 끝내는 플레이(2아웃 뒤 땅볼·뜬공 아웃)는 엔진이 주자를 제자리로 기록한다(득점 무효라 결과는 맞음).
    연출에서는 2아웃이면 타구 순간 모두 뛰므로 세 번째 아웃 순간까지 다음 베이스로 달리게 한다
  - 타구 카메라 중에는 스트라이크 존 네모·커서·투구 위치 링을 숨긴다
  - 검증: 대본 불변식(공이 엔진 지점 통과, 아웃이면 공 먼저/세이프면 주자 먼저, 야수 속도)을 AI 1만여 타구로 확인했다 (Unity 밖에서 스텁으로 컴파일).
    시각 확인은 PlaySnapshots

- 작전·주루 (2026-10-05):
  - 감독은 사람(`HumanManagerDecision`)이지만 교체는 AI: `SubstitutionsByAi = true`. 작전 담당은 모드별로
    `HumanOffenseTactics`(타격 모드: 도루·번트) / `HumanDefenseTactics`(투구 모드: 고의4구). 셋 다 저장된다(저장 형식 v3)
  - 흐름: Unity `TacticsPoint` 멈춤 조건으로 사람 작전 결정 직전(타격 모드 매 투구 전 `OffensePrePitch`, 투구 모드 타석 시작
    `DefenseManager`)에서 멈추고, START 전에 작전 버튼을 받아 대기열에 건 뒤 그 결정 지점을 `Step()`. 이어지는 투구는 START를 다시 묻지 않음
  - 작전 버튼 (START 위 한 줄): STEAL 2B/3B(주자별), SAC BUNT, DRAG BUNT, RUN 성향 / 투구 모드 INTENT. WALK. 상황에 안 맞으면 비활성
  - 번트는 자세 토글: 켜면 SWING 버튼이 BUNT(초록), 같은 타석 동안 유지(2스트라이크·새 타석이면 해제). 스윙 입력이 번트가 되고
    커서·타이밍 품질이 번트 로그 오즈를 보정 (`InputModifierConfig.MaxBuntLogitShift/Penalty` 0.4/0.4, 엔진 `BuntResolver`. AI는 보정 없음)
  - 도루를 건 주자: 3D 모양 주황·리드 크게, HUD 점수판 오른쪽 루상 다이아몬드에도 주황 + "STEAL nB"
  - 주루 성향 `TeamGameState.BaserunningStyle` (보통/공격적/신중): 추가 진루·태그업 필요 여유 시간에
    `BaserunningConfig.AggressiveMarginShiftS`(−0.25) / `CautiousMarginShiftS`(+0.30). AI는 보통(0)이라 하네스 결과 그대로
  - 2아웃 땅볼 연출 버그: 엔진은 이닝 종료 플레이에서 주자를 제자리로 기록 (결과는 맞음). 연출이 세 번째 아웃까지 뛰게 고침

### Unity TUNE 기본값 (2단계 종료 시점)

| 항목 | 기본값 | 위치 |
|---|---|---|
| Contact +bonus / -penalty | 1.00 / 0.40 logit | `ProtoTuning.SwingContactLogitCap` / `SwingContactLogitPenalty` (엔진 기본 0.40 / 0.40) |
| Solid +bonus / -penalty | 1.00 / 0.40 logit | `ProtoTuning.SwingSolidLogitCap` / `SwingSolidLogitPenalty` (엔진 기본 0.40 / 0.40) |
| Spray deg / Launch deg | 20 / 15 | 엔진 `InputModifierConfig` 기본 |
| Release sigma | 0.30 | 엔진 `MaxPitchExecutionSigmaLogShift` |
| Location fx x | 1.00 | 엔진 `LocationEffectScale` |
| Ball slow x | 1.30 | `FlightTimeScale` |
| Perfect ms / Zero ms | 25 / 140 | 타이밍 점수 1 / 0 경계 |
| Calib ms | 0 (기기 저장값 우선) | `DisplayLatencyMs`, 범위 −250~150 |
| Cursor R m | 0.120 | 커서 반지름 |
| Timing weight | 0.60 | 품질 합산에서 타이밍 비중 (커서 0.40) |
| Cursor perfect / zero d/R | 0.30 / 2.00 | 커서 점수 1 / 0 경계 (zero는 2026-10-05 1.20 → 2.00, 터치 조작에서 거의 늘 0이라) |
| Drag sens | 1.20 | |
| Gauge period s | 1.10 | 투구 게이지 왕복 주기 |
| 슬라이더 없음 | LateCutoff 120 ms, HoldTakeMargin 0.10 m, 그림자 켬, 보정 10스윙·300 ms 초과 제외, 평균 창 10스윙 | `ProtoTuning` |

## 나중에 할 일 (남은 이슈)

- 타구 중 실시간 주루 버튼(프로야구9 방식: 타구를 보며 진루·귀루 지시). 엔진에 주루 결정 지점(`IBaserunningDecision`) 추가 필요
- 내야안타가 현실보다 조금 많다 (2026-10-05 측정, Unity 경기 팀 2000경기: 땅볼 중 8.3%, 전체 안타 중 12.2% / 현실 대략 6.5~7%, 9%).
  이날 수비 수정 전후 차이 없음 (8.4% → 8.3%)

- 인플레이 연출 아이폰 실기기 FPS 미확인 (데스크톱 브라우저에서만 약 60 FPS 확인). FX의 Auto play를 켜고 디버그 표시의 FPS로 확인
- 연출 미세 문제: 병살 중계 송구 실책 때 타자 주자가 공보다 늦게 2루에 도착해 보이는 경우가 드물게 있음 (1만 타구 중 4건)

- AI 타자가 스트라이크만 던지는 투수에게 적응하지 못한다. 한가운데만 던져도 Heart 공을 약 28% 그냥 지켜봐서
  루킹 삼진이 나온다 (밸런스 비교 f: 타율 .260, K% 23%). 투수의 존 투구 비율을 보고 스윙 성향을 바꾸는 식으로 개선
- 타구속도 × 발사각 표(`BalanceProbe --ev-la`, 시나리오 a 2000경기)에 남은 어긋남 (참고만):
  - 25~35도 <129 km/h(.19)가 129~145 km/h(.09)보다 조금 높다. 내야·외야 사이 60~80 m에 떨어지는 텍사스 안타 구간으로
    실제 Statcast에도 약하게 있는 현상. 뜬공 BABIP .08(참고 .13)로 낮음
- 2루타가 안타의 16%(현실 약 20%), 2루타/타석 3.3%(참고 4.4%). 외야를 넘어가는 깊은 안타가 적은 탓(뜬공 BABIP와 같은 원인).
  SLG는 목표 −.016으로 허용 범위 안이지만 여유가 적다
- 카메라 원근 때문에 타이밍이 이르게 쏠리는 문제는 개인 보정으로 흡수 중. 근본 대책(잔상·통과 지점 표시)은 보류
- 능력치 화면 표시는 OOTP처럼 1~100 스케일 (엔진 내부 20~80은 그대로, 표시할 때만 변환)
- 나중에 설정에서 능력치 표시 방식(1~100 / 20~80 등)을 고를 수 있게
- WebGL 한글 폰트 없음 (화면 문구 영문)
- 실기기 iOS/Android 네이티브 빌드는 아직 안 함 (WebGL만)

## PowerShell 주의

- Windows PowerShell 5.1에서 `git ... | Select-Object -First N`처럼 네이티브 명령 출력을 중간에 끊으면 `$LASTEXITCODE`가 −1(=255)이 된다.
  배포 스크립트 자체는 성공 시 0을 반환하므로, 뒤에 이런 명령을 이어 붙여 255가 나와도 배포 실패가 아니다

## GameEngine 주요 API

- `new GameEngine(GameSetup, LeagueConfig, seed, ControllerSet, IEventSink)`: 경기 생성
- `Step()`: 결정 지점 하나 처리 후 다음 결정 지점에서 멈춤. 담당자가 사람이면 `AwaitingInput` + `Pending`
- `Submit(PitchCall | BatterAction | ManagerOrders)`: 사람 입력. 규칙 위반은 거절 사유와 함께 `Accepted = false`
- `RunUntil(IStopCondition)`: 조건 충족 / 사람 입력 필요 / 경기 종료까지 진행
- `SimulateUntil(IStopCondition)`: 사람 담당 역할도 그 구간만 AI가 대행, 끝나면 원래 담당자로 복원
- `StopConditions`: 타석·반이닝·이닝 종료, `InningReached`, `ScoringThreat/Chance`, `TeamBatting`, `PlayerUp`,
  `PitchingChangeMoment`, `Any` (진행 요청마다 새로 생성)
- `Save()` / `GameEngine.Load(bytes, config, out SavedGame)`: 어느 결정 지점에서든 저장, `SavedGame.ConfigMatches`로 설정 불일치 확인
- `ControllerSet.Assign(side, IPitchingDecision | IBattingDecision | IManagerDecision)`: 언제든 교체.
  AI는 `AiControllers`, 사람은 `HumanPitchingDecision` / `HumanBattingDecision` / `HumanManagerDecision`
- `HumanManagerDecision`: `Queue(action)`로 작전·교체를 미리 걸어둠(멈추지 않음), AI가 교체할 순간에만
  `Pending`(추천은 `ManagerContext.Suggestion`), `DelegateToAi = true`면 작전·교체 모두 AI
- 기록은 이벤트 로그(`State.Log`, `IEventSink`)에서 `StatsAggregator`로 파생

## Unity 사용 시 주의

- 엔진은 netstandard2.1 + C# 9. `record`·`init` 등 C# 9 런타임 지원이 필요한 문법과 외부 패키지는 쓰지 않는다
- 엔진은 동기식이다. UI는 `RunUntil`/`Step` 결과(`AwaitingInput`)를 보고 입력을 받아 `Submit`한다
  (코루틴·프레임 단위로 나눠 호출해 애니메이션과 맞춘다)
- AI 결정도 반드시 `context.Random`(경기 RNG)만 사용해야 저장/불러오기 후 재현성이 유지된다
- 저장하지 않는 것(UI 쪽 상태): 컨트롤러 객체, `HumanManagerDecision`의 걸어둔 지시·교체 거절 기록,
  진행 중인 `SimulateUntil`/멈춤 조건. 불러온 뒤 담당 방식(`SavedGame.Modes`)대로 새 컨트롤러가 연결된다
- 설정(LeagueConfig)이 바뀐 상태에서 불러오면 이어지는 결과가 달라질 수 있다 (`ConfigMatches` 확인)
- `ProtoTuning` 값은 `Assets/Proto/Scenes/Duel.unity`의 ProtoBootstrap에 직렬화된 값이 코드 기본값보다 우선한다 (씬에 없는 새 필드만 코드 기본값).
  코드 기본값을 바꾸면 씬의 `_tuning` 값도 같이 고친다. TUNE의 RESET은 코드 기본값으로 돌린다
- 사람 입력 → 엔진 값 매핑은 Unity 쪽 `Assets/Proto/Scripts/Quality`에 있다. 계수는 `ProtoTuning`, 보정 상한은 `InputModifierConfig`
- 타격 조작 보정 상한(컨택·정타)은 보상(+)·벌칙(−)이 따로다 (`InputModifierConfig.MaxSwing*LogitShift` / `MaxSwing*LogitPenalty`).
  Unity 기본값 보상 1.0·벌칙 0.4(`ProtoTuning.SwingContactLogitCap`/`SwingSolidLogitCap`/`Swing*LogitPenalty`)를 시작 시 적용.
  엔진 기본값은 둘 다 0.4(하네스 기준)라 그대로 둔다
- 판정은 입력 이벤트 타임스탬프(`InputState.currentTime` 시간축) 기준. 프레임 시각으로 판정하지 않는다
- 드래그 이동량은 EnhancedTouch `Touch.delta`를 쓰지 않고 손가락별 직전 위치 차이로 계산한다
  (`Touch.delta`는 프레임을 넘어가면 직전 기록 delta를 빼는 방식이라 1, −2, 3, −4…로 진동한다. 실기기도 동일)
- 플랫폼 차이는 `IHaptics`(Android 진동 / 그 외 없음)와 `FeedbackSettings` 기본값으로만 갈린다
- WebGL에는 시스템 폰트가 없어 한글이 안 보인다. 화면 문구는 영문 (한글이 필요하면 폰트 에셋 추가)
