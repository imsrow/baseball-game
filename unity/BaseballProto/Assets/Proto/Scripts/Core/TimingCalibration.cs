using System;
using System.Collections.Generic;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 개인 타이밍 보정. 측정을 시작하면 다음 N스윙의 평균 타이밍 오차(보정 전 값)를 재서 Calib ms(DisplayLatencyMs)에 넣는다.
    /// 판정 기준 시각 전체만 옮기므로 구종별 도달 시각 차이는 그대로다.
    /// 값은 기기에 저장한다 (PlayerPrefs, WebGL은 IndexedDB). Calib 슬라이더로 직접 바꾼 값도 잠시 멈추면 저장한다
    /// </summary>
    public sealed class TimingCalibration
    {
        private const string PrefsKey = "TimingCalibMs";

        // 슬라이더를 움직이는 동안 매 프레임 저장하지 않도록, 값이 이 시간만큼 그대로면 저장
        private const double SaveDebounceS = 0.5;

        private readonly ProtoTuning _tuning;
        private readonly List<double> _samples = new List<double>();
        private float _savedValue;
        private float _pendingValue;
        private double _pendingSince;

        public TimingCalibration(ProtoTuning tuning)
        {
            _tuning = tuning;
            Load();
        }

        public bool Measuring { get; private set; }

        /// <summary>측정에 쓴 스윙 수</summary>
        public int Count => _samples.Count;

        public int Target => Math.Max(1, _tuning.CalibSwingCount);

        /// <summary>지금 적용 중인 보정 (ms, − 이르게 치는 습관)</summary>
        public float CurrentMs => _tuning.DisplayLatencyMs;

        /// <summary>마지막 결과 안내 문구와 표시 마감 시각 (ProtoClock)</summary>
        public string Notice { get; private set; } = string.Empty;

        public double NoticeUntil { get; private set; }

        /// <summary>저장된 보정값을 적용 (없으면 지금 값 유지)</summary>
        public void Load()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                _tuning.DisplayLatencyMs = PlayerPrefs.GetFloat(PrefsKey);
            }

            _savedValue = _tuning.DisplayLatencyMs;
            _pendingValue = _savedValue;
        }

        public void Start()
        {
            _samples.Clear();
            Measuring = true;
        }

        public void Cancel()
        {
            _samples.Clear();
            Measuring = false;
            ShowNotice("Timing calib cancelled");
        }

        /// <summary>보정 0으로 되돌리고 저장값을 지운다</summary>
        public void ResetToZero()
        {
            _samples.Clear();
            Measuring = false;
            _tuning.DisplayLatencyMs = 0f;
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
            _savedValue = 0f;
            _pendingValue = 0f;
            ShowNotice("Timing calib reset: 0 ms");
        }

        /// <summary>
        /// 스윙 하나의 보정 전 타이밍 오차(ms, − 이름). 측정 중이 아니거나 너무 동떨어진 값이면 무시.
        /// </summary>
        /// <returns>이 스윙으로 측정이 끝나 보정값이 바뀌었으면 true</returns>
        public bool Add(double rawErrorMs)
        {
            if (!Measuring || Math.Abs(rawErrorMs) > _tuning.CalibMaxAbsErrorMs)
            {
                return false;
            }

            _samples.Add(rawErrorMs);
            if (_samples.Count < Target)
            {
                return false;
            }

            double sum = 0;
            foreach (double sample in _samples)
            {
                sum += sample;
            }

            float value = (float)Math.Round(sum / _samples.Count);
            _tuning.DisplayLatencyMs = value;
            Save(value);
            Measuring = false;
            _samples.Clear();
            ShowNotice(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Timing calib set: {0:+0;-0;0} ms",
                value));
            return true;
        }

        /// <summary>매 프레임: 슬라이더로 바꾼 값이 잠시 그대로면 저장</summary>
        public void Tick(double now)
        {
            float value = _tuning.DisplayLatencyMs;
            if (!Mathf.Approximately(value, _pendingValue))
            {
                _pendingValue = value;
                _pendingSince = now;
                return;
            }

            if (!Mathf.Approximately(value, _savedValue) && now - _pendingSince >= SaveDebounceS)
            {
                Save(value);
            }
        }

        private void Save(float value)
        {
            PlayerPrefs.SetFloat(PrefsKey, value);
            PlayerPrefs.Save();
            _savedValue = value;
            _pendingValue = value;
        }

        private void ShowNotice(string text)
        {
            Notice = text;
            NoticeUntil = ProtoClock.Now + _tuning.CalibResultShowS;
        }
    }
}
