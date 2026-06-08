using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WavelengthTask : BaseTask
{
    [Header("Wave Rendering")]
    [Tooltip("LineRenderer that draws the target wave (static).")]
    [SerializeField] private LineRenderer targetWaveRenderer;
    [Tooltip("LineRenderer that draws the player's adjustable wave.")]
    [SerializeField] private LineRenderer currentWaveRenderer;
    [Tooltip("Width of the wave display area in world units (or pixels if using a canvas).")]
    [SerializeField] private float waveWidth = 10f;
    [Tooltip("Height (amplitude range) in world units. A value of 1 means amplitude 1 = 1 unit.")]
    [SerializeField] private float waveHeightScale = 2f;
    [Tooltip("Number of points used to draw the wave (higher = smoother).")]
    [SerializeField] private int waveResolution = 100;

    [Header("UI Controls")]
    [SerializeField] private Slider amplitudeSlider;
    [SerializeField] private Slider frequencySlider;
    [SerializeField] private TMP_Text amplitudeValueText;
    [SerializeField] private TMP_Text frequencyValueText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button closeButton;

    [Header("Wave Parameters")]
    [Tooltip("Minimum amplitude (height factor).")]
    [SerializeField] private float minAmplitude = 0.2f;
    [Tooltip("Maximum amplitude.")]
    [SerializeField] private float maxAmplitude = 1.5f;
    [Tooltip("Minimum frequency (cycles per waveWidth).")]
    [SerializeField] private float minFrequency = 0.5f;
    [Tooltip("Maximum frequency.")]
    [SerializeField] private float maxFrequency = 3.0f;

    [Header("Tolerance")]
    [Tooltip("Allowed difference in amplitude to consider a match.")]
    [SerializeField] private float amplitudeTolerance = 0.05f;
    [Tooltip("Allowed difference in frequency.")]
    [SerializeField] private float frequencyTolerance = 0.1f;

    [Header("Feedback")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;

   

    private float targetAmplitude;
    private float targetFrequency;

    private float currentAmplitude;
    private float currentFrequency;

    private bool isMatching = false;

    protected override void OnOpen()
    {
        targetAmplitude = Random.Range(minAmplitude, maxAmplitude);
        targetFrequency = Random.Range(minFrequency, maxFrequency);

        currentAmplitude = (minAmplitude + maxAmplitude) / 2f;
        currentFrequency = (minFrequency + maxFrequency) / 2f;

        // Setup sliders
        if (amplitudeSlider)
        {
            amplitudeSlider.minValue = minAmplitude;
            amplitudeSlider.maxValue = maxAmplitude;
            amplitudeSlider.value = currentAmplitude;
            amplitudeSlider.onValueChanged.AddListener(OnAmplitudeChanged);
        }

        if (frequencySlider)
        {
            frequencySlider.minValue = minFrequency;
            frequencySlider.maxValue = maxFrequency;
            frequencySlider.value = currentFrequency;
            frequencySlider.onValueChanged.AddListener(OnFrequencyChanged);
        }

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTask);
        }

        // Draw initial waves
        UpdateValueLabels();
        DrawWave(targetWaveRenderer, targetAmplitude, targetFrequency);
        DrawWave(currentWaveRenderer, currentAmplitude, currentFrequency);

        SetStatus("ADJUST WAVES TO MATCH THE TARGET");
        SetFeedback("", neutralColor);
        isMatching = false;
    }

    protected override void OnClose()
    {
        // Clean up listeners
        if (amplitudeSlider) amplitudeSlider.onValueChanged.RemoveListener(OnAmplitudeChanged);
        if (frequencySlider) frequencySlider.onValueChanged.RemoveListener(OnFrequencyChanged);
    }


    private void OnAmplitudeChanged(float value)
    {
        currentAmplitude = value;
        UpdateValueLabels();
        DrawWave(currentWaveRenderer, currentAmplitude, currentFrequency);
        CheckMatch();
    }

    private void OnFrequencyChanged(float value)
    {
        currentFrequency = value;
        UpdateValueLabels();
        DrawWave(currentWaveRenderer, currentAmplitude, currentFrequency);
        CheckMatch();
    }


    private void CheckMatch()
    {
        if (isMatching) return;

        float ampDiff = Mathf.Abs(currentAmplitude - targetAmplitude);
        float freqDiff = Mathf.Abs(currentFrequency - targetFrequency);

        bool amplitudeMatch = ampDiff <= amplitudeTolerance;
        bool frequencyMatch = freqDiff <= frequencyTolerance;

        if (amplitudeMatch && frequencyMatch)
        {
            isMatching = true;
            SetFeedback("PERFECT MATCH! TASK COMPLETE", correctColor);
            SetStatus("CALIBRATION SUCCESS");
            StartCoroutine(CompleteAfterDelay(1.2f));
        }
        else
        {
            // Show hints
            string hint = "";
            if (!amplitudeMatch) hint += $"Amplitude: {(currentAmplitude > targetAmplitude ? "▼ Too high" : "▲ Too low")}  ";
            if (!frequencyMatch) hint += $"Frequency: {(currentFrequency > targetFrequency ? "▼ Too fast" : "▲ Too slow")}";
            SetFeedback(hint, wrongColor);
        }
    }

    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteTask();
    }


    /// <param name="lr">LineRenderer component.</param>
    /// <param name="amplitude">Height of the wave (peak to trough).</param>
    /// <param name="frequency">Number of full cycles over the total width.</param>
    private void DrawWave(LineRenderer lr, float amplitude, float frequency)
    {
        if (lr == null) return;

        lr.positionCount = waveResolution;

        Vector3[] points = new Vector3[waveResolution];
        float step = waveWidth / (waveResolution - 1);
        float halfWidth = waveWidth * 0.5f;

        // Use local position relative to the LineRenderer's transform.
        // Assume the LineRenderer's origin is at the left‑middle of the display area.
        for (int i = 0; i < waveResolution; i++)
        {
            float x = -halfWidth + i * step;
            float t = (float)i / (waveResolution - 1); // 0..1 across width
            // y = amplitude * sin(2π * frequency * t)
            float y = amplitude * Mathf.Sin(2f * Mathf.PI * frequency * t);
            y *= waveHeightScale;
            points[i] = new Vector3(x, y, 0f);
        }

        lr.SetPositions(points);
    }

    private void UpdateValueLabels()
    {
        if (amplitudeValueText)
            amplitudeValueText.text = $"{currentAmplitude:F2}";
        if (frequencyValueText)
            frequencyValueText.text = $"{currentFrequency:F2}";
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    private void SetFeedback(string msg, Color color)
    {
        if (feedbackText)
        {
            feedbackText.text = msg;
            feedbackText.color = color;
        }
    }
}