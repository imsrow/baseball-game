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

- `dotnet build`, `dotnet test` (xUnit)
- 하네스 (요청 시에만): `dotnet run -c Release --project tools/BaseballSim.Harness -- --seeds 1`
- 밸런스 비교 (사람처럼 던지기·치기 vs AI, 구역별 성적): `dotnet run -c Release --project tools/BaseballSim.BalanceProbe -- --games 2000 --scenarios a,b,f,g,h,i [--swing-caps 1.0] [--swing-penalty 0.4]`
- WebGL 빌드 (에디터를 닫고): `Unity.exe -batchmode -projectPath unity/BaseballProto -buildTarget WebGL -executeMethod BaseballProto.EditorTools.ProtoBuild.BuildWebGLBatch`
  (에디터 메뉴 Baseball > Build WebGL도 같음). 결과물 `unity/BaseballProto/Builds/WebGL`
- WebGL 배포: `powershell -ExecutionPolicy Bypass -File tools/deploy-webgl.ps1` → gh-pages 브랜치 → https://imsrow.github.io/baseball-game/
  - Pages는 Content-Encoding 헤더를 못 붙여 Gzip + Decompression Fallback(JS 해제), 파일명 해시
  - 템플릿 `Assets/WebGLTemplates/BaseballPWA`: iOS 홈 화면 메타, 네트워크 우선 서비스 워커, 렌더 배율 상한 2
  - WebGL은 첫 탭 전 소리가 안 나므로 TAP TO START 후 시작. targetFrameRate는 −1(브라우저 rAF)
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
- 하네스(시드 1): 6개 목표 지표 모두 허용 범위 내. xUnit 테스트 95개 통과
- 투구 위치 효과: 구역(Heart/Shadow/Chase/Waste)별 정타 로그 오즈·타구속도 보정 (`BattedBallConfig.SolidLogitShiftByRegion`,
  `ExitVelocityKmhByRegion`), 전체 배율 `LeagueConfig.LocationEffectScale`(기본 1 = Statcast 근사, Unity TUNE에서 조절).
  Standard 프리셋 구역별 컨택률은 Heart 헛스윙 약 13%, Chase 약 47%로 조정. 목표: Heart 타율 .300·장타율 .550 근처, Chase 타율 .150 근처
- 2단계 진행 중: Unity 1:1 투타 대결 프로토타입 (`unity/BaseballProto`, Unity 6000.6.4f1, URP, 세로 고정)
  - 엔진 최소 확장: `BatterAction.TimingDirection`(이르면 당겨치기), `CursorVerticalOffset`(커서가 위면 발사각 낮게),
    상한은 `InputModifierConfig.MaxTimingSprayShiftDeg` / `MaxCursorLaunchAngleShiftDeg`. AI는 null(중립)이라 하네스 결과 불변
  - 홀드 조작: 커서를 존 밖(테두리 + `ProtoTuning.HoldTakeMarginM`)에서 떼면 스윙 취소(회색 커서) = 볼을 참는 방법
  - 투구 연출: 휨은 u²(중력처럼 점점), 공 바로 아래 바닥 그림자(FX에서 끄기). 포수 뒤 근접 카메라라 공이 마지막 약 50ms에
    화면상 크게 떨어져 보이고, 그 전엔 존 위쪽에 떠 보여 이르게·높게 치는 쏠림이 생긴다 (디버그의 최근 10스윙 평균으로 확인)

## 나중에 할 일

- AI 타자가 스트라이크만 던지는 투수에게 적응하지 못한다. 한가운데만 던져도 Heart 공을 약 28% 그냥 지켜봐서
  루킹 삼진이 나온다 (밸런스 비교 f: 타율 .260, K% 23%). 투수의 존 투구 비율을 보고 스윙 성향을 바꾸는 식으로 개선

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
- 사람 입력 → 엔진 값 매핑은 Unity 쪽 `Assets/Proto/Scripts/Quality`에 있다. 계수는 `ProtoTuning`, 보정 상한은 `InputModifierConfig`
- 타격 조작 보정 상한(컨택·정타)은 보상(+)·벌칙(−)이 따로다 (`InputModifierConfig.MaxSwing*LogitShift` / `MaxSwing*LogitPenalty`).
  Unity 기본값 보상 1.0·벌칙 0.4(`ProtoTuning.SwingContactLogitCap`/`SwingSolidLogitCap`/`Swing*LogitPenalty`)를 시작 시 적용.
  엔진 기본값은 둘 다 0.4(하네스 기준)라 그대로 둔다
- 판정은 입력 이벤트 타임스탬프(`InputState.currentTime` 시간축) 기준. 프레임 시각으로 판정하지 않는다
- 드래그 이동량은 EnhancedTouch `Touch.delta`를 쓰지 않고 손가락별 직전 위치 차이로 계산한다
  (`Touch.delta`는 프레임을 넘어가면 직전 기록 delta를 빼는 방식이라 1, −2, 3, −4…로 진동한다. 실기기도 동일)
- 플랫폼 차이는 `IHaptics`(Android 진동 / 그 외 없음)와 `FeedbackSettings` 기본값으로만 갈린다
- WebGL에는 시스템 폰트가 없어 한글이 안 보인다. 화면 문구는 영문 (한글이 필요하면 폰트 에셋 추가)
