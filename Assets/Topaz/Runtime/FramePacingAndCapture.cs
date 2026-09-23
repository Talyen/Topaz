using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Topaz
{
    /// <summary>Sets desktop presentation pacing and records an opt-in diagnostic baseline.</summary>
    public sealed class FramePacingAndCapture : MonoBehaviour
    {
        const double WarmupSeconds = 10.0;
        const double CaptureSeconds = 60.0;

        readonly List<float> _frameTimesMs = new List<float>(8192);
        double _captureStart;
        bool _captureRequested;

        void Awake()
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;

            foreach (string argument in Environment.GetCommandLineArgs())
            {
                if (argument != "--topaz-perf") continue;
                _captureRequested = true;
                Debug.Log("[Topaz] Performance capture is waiting for an active player window.");
                break;
            }
        }

        void OnApplicationFocus(bool focused)
        {
            if (!_captureRequested) return;
            if (focused) StartWindow();
            else ResetWindow();
        }

        void OnApplicationPause(bool paused)
        {
            if (_captureRequested && paused) ResetWindow();
        }

        void LateUpdate()
        {
            if (!_captureRequested) return;
            if (!Application.isFocused)
            {
                ResetWindow();
                return;
            }

            if (_captureStart <= 0.0) StartWindow();

            double now = Time.realtimeSinceStartupAsDouble;
            if (now < _captureStart) return;

            float frameMs = Time.unscaledDeltaTime * 1000f;
            if (frameMs > 0f) _frameTimesMs.Add(frameMs);

            if (now - _captureStart < CaptureSeconds) return;
            if (_frameTimesMs.Count < 120)
            {
                Debug.LogWarning("[Topaz] Too few foreground frames for a valid report; restarting capture.");
                StartWindow();
                return;
            }

            _captureRequested = false;
            WriteReport();
        }

        void StartWindow()
        {
            _frameTimesMs.Clear();
            _captureStart = Time.realtimeSinceStartupAsDouble + WarmupSeconds;
            Debug.Log("[Topaz] Performance capture will start after a 10-second foreground warm-up.");
        }

        void ResetWindow()
        {
            if (_captureStart > 0.0)
                Debug.Log("[Topaz] Player lost focus; performance capture will restart when active.");
            _captureStart = 0.0;
            _frameTimesMs.Clear();
        }

        void WriteReport()
        {
            if (_frameTimesMs.Count == 0)
            {
                Debug.LogWarning("[Topaz] No frames were captured.");
                return;
            }

            float[] sorted = _frameTimesMs.ToArray();
            Array.Sort(sorted);

            var refresh = Screen.currentResolution.refreshRateRatio;
            double refreshHz = refresh.denominator == 0
                ? 0.0
                : (double)refresh.numerator / refresh.denominator;
            double budgetMs = refreshHz > 0.0 ? 1000.0 / refreshHz : 0.0;

            var report = new PerformanceReport
            {
                capturedAtUtc = DateTime.UtcNow.ToString("o"),
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                processor = SystemInfo.processorType,
                processorCores = SystemInfo.processorCount,
                systemMemoryMb = SystemInfo.systemMemorySize,
                gpu = SystemInfo.graphicsDeviceName,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                resolution = $"{Screen.width}x{Screen.height}",
                refreshHz = refreshHz,
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                sampleSeconds = CaptureSeconds,
                frameCount = sorted.Length,
                p50Ms = Percentile(sorted, 0.50),
                p95Ms = Percentile(sorted, 0.95),
                p99Ms = Percentile(sorted, 0.99),
                longestMs = sorted[sorted.Length - 1],
                framesOver16_67Ms = CountAbove(sorted, 16.67),
                framesOver33_33Ms = CountAbove(sorted, 33.33),
                displayBudgetMs = budgetMs,
                framesOverDisplayBudget = budgetMs > 0.0 ? CountAbove(sorted, budgetMs + 0.5) : 0,
                framesOverTwoRefreshes = budgetMs > 0.0 ? CountAbove(sorted, budgetMs * 2.0 + 0.5) : 0,
                note = "Uninterrupted foreground sample of an empty bootstrap scene; diagnostic tooling check only."
            };

            string directory = Path.Combine(Application.persistentDataPath, "PerformanceReports");
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, $"topaz-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(file, JsonUtility.ToJson(report, true));
            Debug.Log($"[Topaz] Performance report: {file}");
        }

        static float Percentile(float[] sorted, double quantile)
        {
            int index = Math.Max(0, (int)Math.Ceiling(quantile * sorted.Length) - 1);
            return sorted[Math.Min(index, sorted.Length - 1)];
        }

        static int CountAbove(float[] sorted, double threshold)
        {
            int count = 0;
            foreach (float frame in sorted)
                if (frame > threshold) count++;
            return count;
        }

        [Serializable]
        sealed class PerformanceReport
        {
            public string capturedAtUtc;
            public string unityVersion;
            public string platform;
            public string operatingSystem;
            public string processor;
            public int processorCores;
            public int systemMemoryMb;
            public string gpu;
            public int graphicsMemoryMb;
            public string graphicsApi;
            public string resolution;
            public double refreshHz;
            public string qualityLevel;
            public double sampleSeconds;
            public int frameCount;
            public float p50Ms;
            public float p95Ms;
            public float p99Ms;
            public float longestMs;
            public int framesOver16_67Ms;
            public int framesOver33_33Ms;
            public double displayBudgetMs;
            public int framesOverDisplayBudget;
            public int framesOverTwoRefreshes;
            public string note;
        }
    }
}
