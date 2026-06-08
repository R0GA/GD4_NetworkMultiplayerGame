using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : NetworkBehaviour
{
    [Header("Footstep Clips")]
    [Tooltip("Add all your footstep audio clips here (4-6 recommended)")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Timing")]
    [Tooltip("Time in seconds between each footstep at full speed")]
    [SerializeField] private float stepInterval = 0.45f;
    [Tooltip("Only play footsteps if movement input exceeds this threshold")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Pitch Randomisation")]
    [Tooltip("Base pitch — 1 is normal speed")]
    [SerializeField] private float basePitch = 1f;
    [Tooltip("How much pitch can vary up or down randomly each step")]
    [SerializeField] private float pitchVariance = 0.15f;

    [Header("Volume")]
    [Tooltip("Base volume of each footstep")]
    [SerializeField][Range(0f, 1f)] private float stepVolume = 0.6f;
    [Tooltip("How much volume varies randomly each step")]
    [SerializeField][Range(0f, 0.3f)] private float volumeVariance = 0.1f;

    [Header("3D Spatial Settings")]
    [Tooltip("Distance at which footsteps are at full volume")]
    [SerializeField] private float minDistance = 1f;
    [Tooltip("Distance at which footsteps can no longer be heard")]
    [SerializeField] private float maxDistance = 15f;

    private AudioSource audioSource;
    private CharacterController cc;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CharacterController>();

        // Configure 3D spatial audio
        audioSource.spatialBlend = 1f;         // Full 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        // Only the owner drives footstep timing —
        // the AudioSource plays on this object in world space,
        // so all nearby clients hear it spatially via Netcode.
        if (!IsOwner) return;

        bool isMoving = cc.velocity.magnitude > moveThreshold;
        bool isGrounded = cc.isGrounded;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            // Reset timer so first step plays immediately when moving again
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            Debug.LogWarning("[FootstepAudio] No footstep clips assigned!");
            return;
        }

        // Pick a random clip, avoiding repeating the last one
        AudioClip clip = GetRandomClip();

        // Randomise pitch and volume slightly each step
        audioSource.pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);
        float volume = stepVolume + Random.Range(-volumeVariance, volumeVariance);

        audioSource.PlayOneShot(clip, volume);
    }

    private int lastClipIndex = -1;
    private AudioClip GetRandomClip()
    {
        if (footstepClips.Length == 1) return footstepClips[0];

        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        }
        while (index == lastClipIndex); // Avoid repeating same clip twice in a row

        lastClipIndex = index;
        return footstepClips[index];
    }
}