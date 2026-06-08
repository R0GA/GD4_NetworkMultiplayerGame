using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class SlugFootstepAudio : NetworkBehaviour
{
    [Header("Squelch Clips")]
    [Tooltip("Add your 4 squelch audio clips here")]
    [SerializeField] private AudioClip[] squelchClips;

    [Header("Timing")]
    [Tooltip("Time in seconds between each squelch step")]
    [SerializeField] private float stepInterval = 0.5f;
    [Tooltip("Only play sounds if movement speed exceeds this threshold")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Pitch Randomisation")]
    [Tooltip("Base pitch — 1 is normal speed")]
    [SerializeField] private float basePitch = 1f;
    [Tooltip("How much pitch can vary up or down randomly each step")]
    [SerializeField] private float pitchVariance = 0.2f;

    [Header("Volume")]
    [Tooltip("Base volume of each squelch")]
    [SerializeField] [Range(0f, 1f)] private float stepVolume = 0.6f;
    [Tooltip("How much volume varies randomly each step")]
    [SerializeField] [Range(0f, 0.3f)] private float volumeVariance = 0.1f;

    [Header("3D Spatial Settings")]
    [Tooltip("Shorter than the astronaut — slug should feel like a close-range threat")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 8f;

    private AudioSource audioSource;
    private CharacterController cc;
    private SlugPlayer slugPlayer;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CharacterController>();
        slugPlayer = GetComponent<SlugPlayer>();

        // Configure 3D spatial audio
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Silence completely when transformed into a prop
        if (slugPlayer != null && slugPlayer.IsTransformed)
        {
            stepTimer = 0f;
            return;
        }

        bool isMoving = cc.velocity.magnitude > moveThreshold;
        bool isGrounded = cc.isGrounded;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlaySquelch();
                stepTimer = stepInterval;
            }
        }
        else
        {
            // Reset so first step after stopping plays immediately
            stepTimer = 0f;
        }
    }

    private void PlaySquelch()
    {
        if (squelchClips == null || squelchClips.Length == 0)
        {
            Debug.LogWarning("[SlugFootstepAudio] No squelch clips assigned!");
            return;
        }

        AudioClip clip = GetRandomClip();

        audioSource.pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);
        float volume = stepVolume + Random.Range(-volumeVariance, volumeVariance);

        audioSource.PlayOneShot(clip, volume);
    }

    private int lastClipIndex = -1;
    private AudioClip GetRandomClip()
    {
        if (squelchClips.Length == 1) return squelchClips[0];

        int index;
        do
        {
            index = Random.Range(0, squelchClips.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return squelchClips[index];
    }
}
