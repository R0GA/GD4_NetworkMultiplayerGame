using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class AlarmLightEntry
{
    [Tooltip("The Renderer on the light bar mesh (the object with multiple materials).")]
    public Renderer lightRenderer;

    [Tooltip("Index of the material slot that corresponds to the light bar. Usually 1 (the second material).")]
    public int materialIndex = 1;

    [Tooltip("The Light component that is a child of this light prefab. Drag the child light source here.")]
    public Light lightComponent;
}

public class TaskTrigger : MonoBehaviour
{
    [Header("Task")]
    [Tooltip("Must match the taskIdentifier of the desired BaseTask on the player.")]
    [SerializeField] private string taskIdentifier = "";

    [Header("Prompt UI")]
    [SerializeField] private GameObject interactPrompt;

    [Header("Input")]
    [SerializeField] private string interactActionName = "Interact";

    [Header("Alarm Lights")]
    [Tooltip("One entry per warning light prefab in this room. Assign the renderer, material slot index, and child Light component for each.")]
    [SerializeField] private List<AlarmLightEntry> alarmLights = new();
    [Tooltip("Material applied to the light-bar material slot of each renderer when the task completes.")]
    [SerializeField] private Material alarmActiveMaterial;
    [Tooltip("Color set on each child Light component when the alarm activates.")]
    [SerializeField] private Color alarmLightColor = Color.red;
    [Tooltip("How fast the lights pulse (full cycles per second). Set to 0 for solid-on lights.")]
    [SerializeField] private float pulseFrequency = 1.5f;

    [Header("World UI")]
    [Tooltip("Always-visible beacon elements (e.g. the '!' exclamation icon). " +
             "Visible at all times and billboard toward the slug player from anywhere in the level. " +
             "Hidden once the task is complete.")]
    [SerializeField] private List<GameObject> beaconUIElements = new();

    [Tooltip("Proximity-only UI elements (e.g. the interact prompt). " +
             "Only shown when the slug player is inside the trigger zone.")]
    [SerializeField] private List<GameObject> worldUIElements = new();

    public UnityEvent OnInteracted = new();

    // Exposed so TaskManager can look this trigger up by identifier.
    public string TaskIdentifier => taskIdentifier;

    private SlugPlayer playerInRange;
    private InputAction interactAction;

    // Per-entry: a runtime copy of the light-bar material so we drive emission
    // on an instance rather than the shared asset.
    private List<Material> runtimeLightBarMaterials = new();
    private bool alarmActive = false;
    private Coroutine pulseCoroutine;

    // Cached slug player camera transform used for billboarding world UI.
    private Transform slugCameraTransform;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Both beacon and proximity UI start hidden on all clients.
        // The beacon is only revealed once we confirm a locally-owned SlugPlayer
        // exists on this machine (see TryCacheSlugCamera). This ensures the FPS
        // client never sees slug-only UI.
        SetBeaconVisible(false);
        SetWorldUIVisible(false);
    }

    private void Start()
    {
        // Attempt to find the locally-owned slug player's camera. If the slug
        // hasn't spawned yet (common in NGO), we retry in Update until it appears.
        TryCacheSlugCamera();
    }

    private void LateUpdate()
    {
        // If we haven't confirmed a local slug player yet, keep trying each frame.
        // On the FPS client this will never succeed, so beacons stay hidden forever.
        if (slugCameraTransform == null)
        {
            TryCacheSlugCamera();
            return;
        }

        // Billboard all visible world UI elements (beacon + proximity) toward the slug player's camera.
        BillboardList(beaconUIElements);
        BillboardList(worldUIElements);
    }

    private void BillboardList(List<GameObject> elements)
    {
        foreach (var element in elements)
        {
            if (element == null || !element.activeSelf) continue;
            element.transform.rotation = Quaternion.LookRotation(
                element.transform.position - slugCameraTransform.position
            );
        }
    }

    private void SetWorldUIVisible(bool visible)
    {
        foreach (var element in worldUIElements)
            if (element != null) element.SetActive(visible);
    }

    private void SetBeaconVisible(bool visible)
    {
        foreach (var element in beaconUIElements)
            if (element != null) element.SetActive(visible);
    }

    /// <summary>
    /// Finds the local SlugPlayer in the scene and caches their camera transform
    /// for use by the billboard logic. Called at Start and again on trigger enter
    /// in case the player wasn't spawned yet at Start.
    /// </summary>
    private void TryCacheSlugCamera()
    {
        if (slugCameraTransform != null) return;

        // Only the slug client will have a locally-owned SlugPlayer.
        // On the FPS client this loop finds nothing, camera stays null,
        // and beacons remain hidden for that client permanently.
        foreach (var candidate in FindObjectsByType<SlugPlayer>(FindObjectsSortMode.None))
        {
            if (!candidate.IsOwner) continue;
            var cam = candidate.GetComponentInChildren<Camera>();
            slugCameraTransform = cam != null ? cam.transform : candidate.transform;

            // Now that we've confirmed this is the slug client, make the beacon visible.
            SetBeaconVisible(true);
            break;
        }
    }

    // -------------------------------------------------------------------------
    // Trigger enter / exit
    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<SlugPlayer>();
        if (player == null || !player.IsOwner) return;

        BaseTask task = player.GetTaskByIdentifier(taskIdentifier);
        if (task == null || task.IsComplete) return;

        playerInRange = player;

        // Refresh camera cache (handles the case where Start ran before the player spawned).
        var cam = player.GetComponentInChildren<Camera>();
        slugCameraTransform = cam != null ? cam.transform : player.transform;

        // Show proximity UI; beacon is already visible.
        SetWorldUIVisible(true);

        var pi = player.GetComponent<PlayerInput>();
        if (pi != null)
        {
            var action = pi.actions.FindAction(interactActionName, throwIfNotFound: false);
            if (action != null)
            {
                interactAction = action;
                interactAction.performed += OnInteractPerformed;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<SlugPlayer>();
        if (player == null || !player.IsOwner) return;
        if (player != playerInRange) return;
        CleanUp();
    }

    // -------------------------------------------------------------------------
    // Interaction
    // -------------------------------------------------------------------------

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (playerInRange == null) return;
        if (playerInRange.GetUIMode()) return; // already in another UI

        BaseTask task = playerInRange.GetTaskByIdentifier(taskIdentifier);
        if (task == null || task.IsComplete)
        {
            CleanUp();
            return;
        }

        TryOpenTask();
    }

    public void TryOpenTask()
    {
        if (playerInRange == null) return;
        BaseTask task = playerInRange.GetTaskByIdentifier(taskIdentifier);
        if (task == null) return;

        if (task.TryOpen(playerInRange))
        {
            OnInteracted?.Invoke();
            SetWorldUIVisible(false);
            SetBeaconVisible(false);
        }
    }

    // -------------------------------------------------------------------------
    // Alarm lights — called by TaskManager on all clients when the task completes
    // -------------------------------------------------------------------------

    /// <summary>
    /// Swaps the correct material slot on each light-bar renderer to the alarm
    /// material, recolors each child Light component, and starts the pulse.
    /// Safe to call multiple times; subsequent calls are ignored.
    /// </summary>
    public void ActivateAlarmLights()
    {
        if (alarmActive) return;
        if (alarmActiveMaterial == null || alarmLights.Count == 0) return;

        alarmActive = true;
        runtimeLightBarMaterials.Clear();

        foreach (var entry in alarmLights)
        {
            if (entry.lightRenderer == null) continue;

            // Build a new materials array with only the target slot swapped.
            Material[] mats = entry.lightRenderer.materials; // returns copies
            if (entry.materialIndex < 0 || entry.materialIndex >= mats.Length)
            {
                Debug.LogWarning($"[TaskTrigger] materialIndex {entry.materialIndex} is out of range for renderer '{entry.lightRenderer.name}' which has {mats.Length} material(s). Skipping.");
                runtimeLightBarMaterials.Add(null);
                continue;
            }

            // Instantiate the alarm material so emission can be driven per-instance.
            Material instance = new Material(alarmActiveMaterial);
            mats[entry.materialIndex] = instance;
            entry.lightRenderer.materials = mats;
            runtimeLightBarMaterials.Add(instance);

            // Set the child Light color and switch it on.
            if (entry.lightComponent != null)
            {
                entry.lightComponent.color = alarmLightColor;
                entry.lightComponent.enabled = true;
            }
        }

        if (pulseFrequency > 0f)
            pulseCoroutine = StartCoroutine(PulseLights());
    }

    private IEnumerator PulseLights()
    {
        // Cache whether each entry's material supports emission so we don't
        // call HasProperty every frame.
        bool[] hasEmission = new bool[runtimeLightBarMaterials.Count];
        Color[] baseEmission = new Color[runtimeLightBarMaterials.Count];
        float[] baseLightIntensity = new float[alarmLights.Count];

        for (int i = 0; i < runtimeLightBarMaterials.Count; i++)
        {
            var mat = runtimeLightBarMaterials[i];
            if (mat != null && mat.HasProperty("_EmissionColor"))
            {
                hasEmission[i] = true;
                baseEmission[i] = mat.GetColor("_EmissionColor");
            }
        }

        for (int i = 0; i < alarmLights.Count; i++)
        {
            if (alarmLights[i].lightComponent != null)
                baseLightIntensity[i] = alarmLights[i].lightComponent.intensity;
        }

        while (true)
        {
            float t = (Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;

            // Pulse the light-bar material emission.
            for (int i = 0; i < runtimeLightBarMaterials.Count; i++)
            {
                if (!hasEmission[i] || runtimeLightBarMaterials[i] == null) continue;
                runtimeLightBarMaterials[i].SetColor("_EmissionColor", baseEmission[i] * t);
            }

            // Pulse the actual Light component intensity in sync.
            for (int i = 0; i < alarmLights.Count; i++)
            {
                var light = alarmLights[i].lightComponent;
                if (light == null) continue;
                light.intensity = baseLightIntensity[i] * t;
            }

            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    private void CleanUp()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }
        playerInRange = null;

        // Hide proximity UI when the player leaves, but leave the beacon visible
        // and keep slugCameraTransform so billboarding continues out of range.
        SetWorldUIVisible(false);
    }

    private void OnDestroy()
    {
        CleanUp();
        slugCameraTransform = null;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        // Clean up runtime material instances to avoid memory leaks.
        foreach (var mat in runtimeLightBarMaterials)
        {
            if (mat != null) Destroy(mat);
        }
    }
}