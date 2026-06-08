using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : NetworkBehaviour
{
    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Timing")]
    [SerializeField] private float stepInterval = 0.45f;
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Pitch Randomisation")]
    [SerializeField] private float basePitch = 1f;
    [SerializeField] private float pitchVariance = 0.15f;

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float stepVolume = 0.6f;
    [SerializeField][Range(0f, 0.3f)] private float volumeVariance = 0.1f;

    [Header("3D Spatial Settings")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;

    private AudioSource audioSource;
    private CharacterController cc;
    private float stepTimer;
    private int lastClipIndex = -1;
    private Vector3 lastPosition;
    private float currentSpeed;
    private bool positionInitialised;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CharacterController>();

        // Spatial settings for when OTHER clients hear this character.
        // The owner's local playback bypasses spatial audio entirely (see PlayLocalFootstep).
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public override void OnNetworkSpawn()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Skip first frame to avoid a spurious step on spawn
        if (!positionInitialised)
        {
            lastPosition = transform.position;
            positionInitialised = true;
            return;
        }

        currentSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        bool isMoving = currentSpeed > moveThreshold;
        bool isGrounded = cc.isGrounded;

        if (isMoving && isGrounded)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                int clipIndex = GetRandomClipIndex();
                float pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);
                float volume = Mathf.Clamp(stepVolume + Random.Range(-volumeVariance, volumeVariance), 0f, 1f);

                // Play immediately, locally, in 2D for the owner.
                // The AudioListener is on top of us so spatial audio would always be max volume
                // regardless of maxDistance — this bypasses that problem cleanly.
                PlayLocalFootstep(clipIndex, pitch, volume);

                // Broadcast to all OTHER clients so they hear it spatially from this object's position.
                if (IsServer)
                    PlayFootstepClientRpc(clipIndex, pitch, volume);
                else
                    RequestFootstepServerRpc(clipIndex, pitch, volume);

                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    // Owner hears their own footsteps locally in 2D — no spatial calculation needed.
    private void PlayLocalFootstep(int clipIndex, float pitch, float volume)
    {
        if (footstepClips == null || clipIndex >= footstepClips.Length) return;

        // Temporarily override spatialBlend to 0 (2D) for this one shot,
        // then restore it so remote clients still hear it spatially.
        float previousBlend = audioSource.spatialBlend;
        audioSource.spatialBlend = 0f;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(footstepClips[clipIndex], volume);
        audioSource.spatialBlend = previousBlend;
    }

    // Called by a pure client — relays up to the server
    [ServerRpc]
    private void RequestFootstepServerRpc(int clipIndex, float pitch, float volume)
    {
        PlayFootstepClientRpc(clipIndex, pitch, volume);
    }

    // Called by the server (directly or via relay) — broadcasts to all clients.
    // The owner skips playback here because they already played it locally above.
    [ClientRpc]
    private void PlayFootstepClientRpc(int clipIndex, float pitch, float volume)
    {
        // Owner already played their own step locally — avoid doubling up.
        if (IsOwner) return;

        if (footstepClips == null || clipIndex >= footstepClips.Length) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(footstepClips[clipIndex], volume);
    }

    private int GetRandomClipIndex()
    {
        if (footstepClips == null || footstepClips.Length == 0) return 0;
        if (footstepClips.Length == 1) return 0;

        int index;
        do { index = Random.Range(0, footstepClips.Length); }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return index;
    }
}