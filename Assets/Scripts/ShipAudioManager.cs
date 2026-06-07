using System.Collections;
using UnityEngine;

/// <summary>
/// Sits on a persistent, non-networked ship GameObject (e.g. "ShipAudioManager").
/// Provides a singleton so TaskManager can trigger the alarm from any client
/// without depending on a NetworkObject that could be owned by only one player.
/// </summary>
public class ShipAudioManager : MonoBehaviour
{
    public static ShipAudioManager Instance { get; private set; }

    [Header("Alarm")]
    [Tooltip("AudioSource for the ship-wide alarm. Set Spatial Blend to 0 (fully 2D) so both players hear it at equal volume regardless of position.")]
    [SerializeField] private AudioSource alarmSource;
    [Tooltip("Clip played when any task is completed.")]
    [SerializeField] private AudioClip alarmClip;
    [Tooltip("How long the alarm plays before fading out. Set to 0 to loop until all tasks are done.")]
    [SerializeField] private float alarmDuration = 10f;

    private Coroutine fadeCoroutine;
    private float originalVolume;

    private void Awake()
    {
        // Simple singleton — only one ship per scene so no need for
        // DontDestroyOnLoad unless you have a persistent scene setup.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (alarmSource != null)
            originalVolume = alarmSource.volume;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Plays the alarm on this client. Called by TaskManager inside its
    /// NetworkVariable callback, which already fires on every client.
    /// </summary>
    public void PlayAlarm()
    {
        if (alarmSource == null || alarmClip == null)
        {
            Debug.LogWarning("[ShipAudioManager] Alarm AudioSource or clip not assigned.");
            return;
        }

        // Cancel any in-progress fade so we restart cleanly.
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
            alarmSource.volume = originalVolume;
        }

        alarmSource.clip = alarmClip;
        alarmSource.loop = (alarmDuration <= 0f);
        alarmSource.Play();

        if (alarmDuration > 0f)
            fadeCoroutine = StartCoroutine(FadeOut(alarmDuration));
    }

    /// <summary>
    /// Stops the alarm immediately (e.g. if the game ends).
    /// </summary>
    public void StopAlarm()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        if (alarmSource != null)
        {
            alarmSource.Stop();
            alarmSource.volume = originalVolume;
        }
    }

    private IEnumerator FadeOut(float duration)
    {
        // Wait for most of the alarm to play, then spend the last second fading.
        float waitTime = Mathf.Max(0f, duration - 1f);
        yield return new WaitForSeconds(waitTime);

        float elapsed = 0f;
        float startVolume = alarmSource.volume;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            alarmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed);
            yield return null;
        }

        alarmSource.Stop();
        alarmSource.volume = originalVolume;
        fadeCoroutine = null;
    }
}