using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class LocalShootAudio : NetworkBehaviour
{
    [Header("Shoot Clips")]
    [Tooltip("Add one or more shoot sound clips — multiple adds slight variation")]
    [SerializeField] private AudioClip[] shootClips;

    [Header("Pitch Randomisation")]
    [SerializeField] private float basePitch = 1f;
    [SerializeField] private float pitchVariance = 0.08f;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.8f;

    private AudioSource audioSource;
    private PlayerInput pi;
    private InputAction shootAction;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // 2D — fully local, no spatial falloff, slug cannot hear this
        //audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public override void OnNetworkSpawn()
    {
        // Non-owners never hear this — matches how NetworkGun disables itself
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        pi = GetComponent<PlayerInput>();
        shootAction = pi.actions["Shoot"];
        shootAction.Enable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (shootAction.WasPressedThisFrame())
            PlayShootSound();
    }

    private void PlayShootSound()
    {
        if (shootClips == null || shootClips.Length == 0)
        {
            Debug.LogWarning("[LocalShootAudio] No shoot clips assigned!");
            return;
        }

        AudioClip clip = shootClips[Random.Range(0, shootClips.Length)];
        audioSource.pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);
        audioSource.PlayOneShot(clip, shootVolume);
    }
}
