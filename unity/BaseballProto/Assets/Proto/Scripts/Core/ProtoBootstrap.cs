using System.Collections.Generic;
using BaseballProto.Feedback;
using BaseballProto.Input;
using BaseballProto.UI;
using BaseballProto.View;
using BaseballSim.Engine.Config;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 씬 진입점. 카메라·조명·경기장·입력·HUD를 코드로 만들고 대결 루프를 시작한다.
    /// 프레임 순서: 입력 이벤트 수집 → HUD 우선 처리 → 남은 입력을 타격/투구로 → 연출 갱신 → (코루틴) 엔진 진행
    /// </summary>
    public sealed class ProtoBootstrap : MonoBehaviour
    {
        private const float FpsSmoothing = 0.1f;

        // 다음 공 시작용 "화면 탭" 판정: 짧게 눌렀다 뗀 것만 (드래그로 커서를 맞추는 동작과 구분)
        private const double TapMaxDurationS = 0.35;
        private const float TapMaxMoveHud = 24f;

        [SerializeField] private ProtoTuning _tuning = new ProtoTuning();

        private readonly List<PointerEvent> _events = new List<PointerEvent>();
        private readonly Dictionary<int, PointerEvent> _tapDowns = new Dictionary<int, PointerEvent>();

        private LeagueConfig _config;
        private TouchHub _touch;
        private PresentationClock _clock;
        private CameraShake _shake;
        private FieldView _field;
        private BatView _bat;
        private BattedBallFlight _flight;
        private RingView _cursorRing;
        private BallView _ball;
        private BattingInput _batting;
        private PitchingInput _pitching;
        private DuelController _duel;
        private Hud _hud;
        private HudRenderer _hudRenderer;
        private CursorTrace _trace;
        private PlateMapper _mapper;
        private float _fps = 60f;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        // WebGL(특히 iOS 사파리)은 첫 터치 전에 소리가 나지 않으므로 첫 탭을 받은 뒤 시작한다
        private bool _awaitingStart;

        private void Awake()
        {
            // WebGL은 브라우저 화면 갱신(requestAnimationFrame)에 맞추는 −1이 가장 매끄럽다 (아이폰 사파리 60Hz).
            // 고정값을 주면 setTimeout 기반으로 바뀌어 프레임이 들쭉날쭉해진다
            bool web = Application.platform == RuntimePlatform.WebGLPlayer;
            Application.targetFrameRate = web ? -1 : _tuning.TargetFrameRate;
            if (!web)
            {
                // 세로·가로 모두 지원: 폰 방향에 따라 자동 회전 (거꾸로 세로는 제외).
                // WebGL은 브라우저가 방향을 정하므로 호출하지 않는다 (호출하면 오류만 난다)
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }

            _awaitingStart = web;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _config = new LeagueConfig();

            // 타격 조작 보정 상한은 Unity 기본값으로 (엔진 기본값은 하네스 기준이라 그대로 둔다)
            _config.InputModifier.MaxSwingContactLogitShift = _tuning.SwingContactLogitCap;
            _config.InputModifier.MaxSwingSolidLogitShift = _tuning.SwingSolidLogitCap;
            _config.InputModifier.MaxSwingContactLogitPenalty = _tuning.SwingContactLogitPenalty;
            _config.InputModifier.MaxSwingSolidLogitPenalty = _tuning.SwingSolidLogitPenalty;
            _clock = new PresentationClock();

            Camera camera = CreateCamera();
            CreateLight();
            var root = new GameObject("Field").transform;
            _field = new FieldView(root, camera, _config.StrikeZone, _tuning);
            _shake = new CameraShake(camera.transform);

            var ball = new BallView(root, _tuning.BallVisualDiameterM, _tuning.BallShadowDiameterM);
            _ball = ball;
            _bat = new BatView(root);
            _flight = new BattedBallFlight(ball, _tuning);
            _cursorRing = new RingView(root, "Cursor", _tuning.CursorRadiusM, ProtoColors.Cursor);
            var targetRing = new RingView(root, "Target", 0.04f, ProtoColors.Target);
            var actualRing = new RingView(root, "Actual", 0.045f, ProtoColors.Actual);
            targetRing.Hide();
            actualRing.Hide();

            IHaptics haptics = PlatformHaptics.Create();
            var impact = new ImpactFeedback(haptics, new SoundBank(camera.gameObject), _clock, _shake,
                FeedbackSettings.ForCurrentPlatform(haptics));

            var mapper = new PlateMapper(camera);
            _mapper = mapper;
            _trace = new CursorTrace();
            _trace.SetEnabled(Application.isEditor);
            _touch = new TouchHub();
            _batting = new BattingInput(_tuning, _config.StrikeZone, mapper,
                screen => _hud != null && _hud.IsOnSwingButton(screen), screen => _hud == null || _hud.IsInDragArea(screen));
            _pitching = new PitchingInput(_tuning, mapper);
            _duel = new DuelController(this, _config, _tuning, _field, ball, _bat, _flight, targetRing, actualRing, _batting,
                _pitching, impact);
            _hud = new Hud(_duel, _batting, _pitching, _tuning, impact, _trace);
            _hudRenderer = new HudRenderer();
        }

        private void Start()
        {
            if (!_awaitingStart)
            {
                StartCoroutine(_duel.Run());
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _fps = Mathf.Lerp(_fps, dt > 0f ? 1f / dt : _fps, FpsSmoothing);
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                _field.ApplyCamera();
            }

            _hud.Layout();
            _events.Clear();
            _touch.Drain(_events);
            if (_awaitingStart)
            {
                WaitForFirstTap();
                return;
            }

            int moveEvents = 0;
            Vector2 moveDelta = Vector2.zero;
            foreach (PointerEvent e in _events)
            {
                if (_hud.Handle(e))
                {
                    _tapDowns.Remove(e.FingerId);
                    continue;
                }

                if (HandleReadyInput(e))
                {
                    continue;
                }

                if (_duel.Mode == DuelMode.Batting)
                {
                    if (e.Phase == PointerPhase.Move)
                    {
                        moveEvents++;
                        moveDelta += e.Delta;
                    }

                    _batting.Handle(e);
                }
                else
                {
                    _pitching.Handle(e);
                }
            }

            _ball.ShadowEnabled = _tuning.BallShadow;
            _clock.Tick(dt);
            _shake.Tick(dt);
            _bat.Tick(_clock.DeltaTime);
            _flight.Tick(_clock.DeltaTime);
            UpdateCursor();
            RecordCursor(moveEvents, moveDelta);
        }

        /// <summary>
        /// 다음 공 대기 중이면 짧은 탭(또는 스페이스)으로 시작한다. 드래그는 그대로 커서 조정으로 넘긴다.
        /// </summary>
        /// <returns>이벤트를 여기서 소비했으면 true</returns>
        private bool HandleReadyInput(PointerEvent e)
        {
            switch (e.Phase)
            {
                case PointerPhase.SwingKey:
                    if (_duel.AwaitingReady)
                    {
                        _duel.RequestPitch();
                        return true;
                    }

                    return false;

                case PointerPhase.Down:
                    _tapDowns[e.FingerId] = e;
                    return false;

                case PointerPhase.Up:
                    if (_tapDowns.TryGetValue(e.FingerId, out PointerEvent down))
                    {
                        _tapDowns.Remove(e.FingerId);
                        float moved = (UiScale.FromScreen(e.ScreenPosition) - UiScale.FromScreen(down.ScreenPosition)).magnitude;
                        if (_duel.AwaitingReady && e.Time - down.Time <= TapMaxDurationS && moved <= TapMaxMoveHud)
                        {
                            _duel.RequestPitch();
                        }
                    }

                    return false;

                default:
                    return false;
            }
        }

        private void WaitForFirstTap()
        {
            foreach (PointerEvent e in _events)
            {
                if (e.Phase == PointerPhase.Down || e.Phase == PointerPhase.SwingKey)
                {
                    // 이 탭(브라우저 사용자 제스처)에서 Unity가 오디오를 깨운다
                    _awaitingStart = false;
                    StartCoroutine(_duel.Run());
                    return;
                }
            }
        }

        private void RecordCursor(int moveEvents, Vector2 moveDelta)
        {
            if (!_trace.Enabled || _duel.Mode != DuelMode.Batting)
            {
                return;
            }

            Vector2 cursor = _batting.Cursor;
            Vector3 screen = _field.Camera.WorldToScreenPoint(new Vector3(cursor.x, cursor.y, 0f));
            _trace.Record(Time.frameCount, ProtoClock.Now, moveEvents, moveDelta, cursor, screen, _mapper.MetersPerPixel(),
                _tuning.DragSensitivity);
        }

        private void OnGUI()
        {
            if (_awaitingStart)
            {
                _hudRenderer.DrawStartOverlay();
                return;
            }

            _hudRenderer.Draw(_hud, _touch.LastEventLagMs, _fps);
        }

        private void OnDestroy()
        {
            _touch?.Dispose();
            _trace?.Dispose();
        }

        private void UpdateCursor()
        {
            bool tap = _batting.Mode == BattingControlMode.TapToSwing;
            if (_duel.Mode != DuelMode.Batting || (tap && !_batting.HasTapMark))
            {
                _cursorRing.Hide();
                return;
            }

            _cursorRing.SetRadius(_tuning.CursorRadiusM);
            // 홀드 모드에서 누르고 있으면 주황: 손을 떼면 스윙된다는 표시. 존 밖이면 회색: 떼면 스윙 취소
            Color color = tap ? ProtoColors.CursorTap
                : _batting.InTakeArea ? ProtoColors.CursorTake
                : _batting.Holding ? ProtoColors.CursorHold
                : ProtoColors.Cursor;
            _cursorRing.SetColor(color);
            _cursorRing.Show(_batting.Cursor);
        }

        private static Camera CreateCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ProtoColors.Sky;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            return camera;
        }

        private static void CreateLight()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var go = new GameObject("Sun");
            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
