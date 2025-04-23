using UnityEngine;

using System.Collections.Generic;

using Unity.Profiling;


public class AdvancedFPSCounter : MonoBehaviour
{
    [Header("Settings")]
    public float updateInterval = 0.5f;
    public float outlierThreshold = 2.5f; // 표준편차 기준 임계값

    [Header("Display")]
    public Color textColor = Color.white;
    public int fontSize = 20;
    public Vector2 position = new Vector2(5, 5);

    private float accum;
    private int frames;
    private float timeleft;
    private float currentFPS;
    private Queue<float> fpsHistory = new Queue<float>();
    private const int MAX_HISTORY = 120; // 60초 * 2 samples/sec
    private GUIStyle textStyle = new GUIStyle();

    private ProfilerRecorder totalReservedMemoryRecorder;
    private ProfilerRecorder gcReservedMemoryRecorder;

    void Start()
    {
        timeleft = updateInterval;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = textColor;
        textStyle.fontSize = fontSize;

        // 메모리 프로파일러 시작
        totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
        gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        if (timeleft <= 0f)
        {
            currentFPS = accum / frames;
            UpdateHistory(currentFPS);

            timeleft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    void UpdateHistory(float newFPS)
    {
        fpsHistory.Enqueue(newFPS);
        while (fpsHistory.Count > MAX_HISTORY)
            fpsHistory.Dequeue();
    }

    void CalculateStats(out float min, out float max, out float avg)
    {
        var validSamples = new List<float>(fpsHistory);

        // 이상치 제거
        if (validSamples.Count > 10)
        {
            float mean = 0f, stdDev = 0f;

            foreach (var fps in validSamples) mean += fps;
            mean /= validSamples.Count;

            foreach (var fps in validSamples)
                stdDev += (fps - mean) * (fps - mean);
            stdDev = Mathf.Sqrt(stdDev / validSamples.Count);

            validSamples.RemoveAll(f => Mathf.Abs(f - mean) > outlierThreshold * stdDev);
        }

        min = float.MaxValue;
        max = float.MinValue;
        avg = 0f;

        foreach (var fps in validSamples)
        {
            if (fps < min) min = fps;
            if (fps > max) max = fps;
            avg += fps;
        }

        avg = validSamples.Count > 0 ? avg / validSamples.Count : 0f;
    }

    void OnGUI()
    {
        CalculateStats(out float min, out float max, out float avg);

        // 메모리 사용량 가져오기
        long totalReservedMemory = totalReservedMemoryRecorder.LastValue / (1024 * 1024); // MB 단위로 변환
        long gcReservedMemory = gcReservedMemoryRecorder.LastValue / (1024 * 1024); // MB 단위로 변환

        string stats = $"Current: {currentFPS:F1} FPS\n"
                     + $"Min: {min:F1}\n"
                     + $"Max: {max:F1}\n"
                     + $"Avg: {avg:F1}\n"
                     + $"Total Reserved Memory: {totalReservedMemory} MB\n"
                     + $"GC Reserved Memory: {gcReservedMemory} MB";

        GUI.Label(new Rect(position.x, position.y, 300, 150), stats, textStyle);
    }

    void OnDisable()
    {
        // 메모리 프로파일러 정리
        totalReservedMemoryRecorder.Dispose();
        gcReservedMemoryRecorder.Dispose();
    }
}
