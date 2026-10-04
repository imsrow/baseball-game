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

        [SerializeField] private ProtoTuning _tuning = new ProtoTuning();

        private readonly List<PointerEvent> _events = new List<PointerEvent>();

        private LeagueConfig _config;
        private TouchHub _touch;
        private PresentationClock _clock;
        private CameraShake _shake;
        private FieldView _field;
        private BatView _bat;
        private BattedBallFlight _flight;
        private RingView _cursorRing;
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

        private void Awake()
        {
            Application.targetFrameRate = _tuning.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _config = new LeagueConfig();
            _clock = new PresentationClock();

            Camera camera = CreateCamera();
            CreateLight();
            var root = new GameObject("Field").transform;
            _field = new FieldView(root, camera, _config.StrikeZone, _tuning);
            _shake = new CameraShake(camera.transform);

            var ball = new BallView(root, _tuning.BallVisualDiameterM);
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
            _batting = new BattingInput(_tuning, mapper, screen => _hud != null && _hud.IsOnSwingButton(screen));
            _pitching = new PitchingInput(_tuning, mapper);
            _duel = new DuelController(this, _config, _tuning, _field, ball, _bat, _flight, targetRing, actualRing, _batting,
                _pitching, impact);
            _hud = new Hud(_duel, _batting, _pitching, _tuning, impact, _trace);
            _hudRenderer = new HudRenderer();
        }

        private void Start()
        {
            StartCoroutine(_duel.Run());
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
            int moveEvents = 0;
            Vector2 moveDelta = Vector2.zero;
            foreach (PointerEvent e in _events)
            {
                if (_hud.Handle(e))
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

            _clock.Tick(dt);
            _shake.Tick(dt);
            _bat.Tick(_clock.DeltaTime);
            _flight.Tick(_clock.DeltaTime);
            UpdateCursor();
            RecordCursor(moveEvents, moveDelta);
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
            _cursorRing.SetColor(tap ? ProtoColors.CursorTap : ProtoColors.Cursor);
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
