using System.Collections;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Lights Out – 1D row variant.
///
/// Automatically spawns <switchCount> copies of switchPrefab into switchRowParent
/// and <switchCount> copies of lightPrefab into lightRowParent when the task opens.
///
/// Switch prefab structure expected:
///   SwitchRoot  [Button]
///     └─ LeverHandle  [Image, RectTransform]   ← first child; slides up/down
///
/// Light prefab structure expected:
///   LightRoot  [Image]   (or Image on any first-found child)
/// </summary>
public class LightsOutTask : BaseTask
{
    // -----------------------------------------------------------------------
    //  Inspector
    // -----------------------------------------------------------------------

    [Header("Lights Out – Prefabs")]
    [Tooltip("Prefab instantiated once per switch. Must have a Button on its root and " +
             "an Image on its first child (the lever handle that slides).")]
    [SerializeField] private GameObject switchPrefab;

    [Tooltip("Prefab instantiated once per light. Must have an Image on its root or " +
             "first child.")]
    [SerializeField] private GameObject lightPrefab;

    [Header("Lights Out – Row")]
    [Tooltip("How many switches/lights to spawn.")]
    [SerializeField] private int switchCount = 7;

    [Tooltip("Parent RectTransform for the switch row. " +
             "Add a Horizontal Layout Group here — the script manages child count.")]
    [SerializeField] private Transform switchRowParent;

    [Tooltip("Parent RectTransform for the light row. " +
             "Add a Horizontal Layout Group here — the script manages child count.")]
    [SerializeField] private Transform lightRowParent;

    [Header("Lights Out – Switch visuals")]
    [Tooltip("anchoredPosition.y of the lever handle when the light is ON (lever up).")]
    [SerializeField] private float leverUpY = 18f;

    [Tooltip("anchoredPosition.y of the lever handle when the light is OFF (lever down).")]
    [SerializeField] private float leverDownY = -18f;

    [Tooltip("Seconds for the lever to slide between positions.")]
    [SerializeField] private float leverAnimDuration = 0.1f;

    [Tooltip("Optional sprite applied to the lever Image when its light is ON.")]
    [SerializeField] private Sprite leverOnSprite;

    [Tooltip("Optional sprite applied to the lever Image when its light is OFF.")]
    [SerializeField] private Sprite leverOffSprite;

    [Header("Lights Out – Light visuals")]
    [Tooltip("Color applied to each light Image while it is ON.")]
    [SerializeField] private Color lightOnColor = new Color(1f, 0.55f, 0.1f);

    [Tooltip("Color applied to each light Image while it is OFF.")]
    [SerializeField] private Color lightOffColor = new Color(0.08f, 0.10f, 0.13f);

    [Header("Lights Out – Puzzle")]
    [Tooltip("Minimum random lever presses used to shuffle from the all-ON state.")]
    [SerializeField] private int minShuffleSteps = 6;

    [Tooltip("Maximum random lever presses used to shuffle from the all-ON state.")]
    [SerializeField] private int maxShuffleSteps = 16;

    [Tooltip("Puzzle seed. -1 = random each round. Fixed value = repeatable puzzle.")]
    [SerializeField] private int randomSeed = -1;

    [Header("Lights Out – Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip leverClickSound;
    [SerializeField] private AudioClip puzzleSolvedSound;

    // -----------------------------------------------------------------------
    //  Private runtime state
    // -----------------------------------------------------------------------

    private bool[] lightState;
    private Button[] switchButtons;
    private RectTransform[] leverHandles;
    private Image[] leverImages;
    private Image[] lightImages;

    private bool leverAnimating;

    // -----------------------------------------------------------------------
    //  BaseTask overrides
    // -----------------------------------------------------------------------

    protected override void OnOpen()
    {
        SpawnRow();
        GeneratePuzzle();
        RefreshAllVisuals(instant: true);
    }

    protected override void OnClose() { }

    // -----------------------------------------------------------------------
    //  Row spawning
    // -----------------------------------------------------------------------

    /// <summary>
    /// Destroys any existing children in both row parents, then instantiates
    /// exactly <switchCount> copies of each prefab and caches component refs.
    /// </summary>
    private void SpawnRow()
    {
        if (!ValidatePrefabs()) return;

        ClearChildren(switchRowParent);
        ClearChildren(lightRowParent);

        lightState = new bool[switchCount];
        switchButtons = new Button[switchCount];
        leverHandles = new RectTransform[switchCount];
        leverImages = new Image[switchCount];
        lightImages = new Image[switchCount];

        for (int i = 0; i < switchCount; i++)
        {
            // ── Switch ──────────────────────────────────────────────────
            GameObject sw = Instantiate(switchPrefab, switchRowParent);
            sw.name = $"Switch_{i:00}";

            switchButtons[i] = sw.GetComponent<Button>();
            if (switchButtons[i] == null)
                Debug.LogWarning($"[LightsOutTask] Switch prefab is missing a Button component.");

            // Wire button listener
            if (switchButtons[i] != null)
            {
                int captured = i;
                switchButtons[i].onClick.RemoveAllListeners();
                switchButtons[i].onClick.AddListener(() => OnLeverPressed(captured));
            }

            // Lever handle = first child of the switch root
            if (sw.transform.childCount > 0)
            {
                Transform handle = sw.transform.GetChild(0);
                leverHandles[i] = handle.GetComponent<RectTransform>();
                leverImages[i] = handle.GetComponent<Image>();

                if (leverHandles[i] == null)
                    Debug.LogWarning($"[LightsOutTask] Switch prefab's first child has no RectTransform.");
            }
            else
            {
                Debug.LogWarning($"[LightsOutTask] Switch prefab needs at least one child (the lever handle).");
            }

            // ── Light ────────────────────────────────────────────────────
            GameObject lt = Instantiate(lightPrefab, lightRowParent);
            lt.name = $"Light_{i:00}";

            lightImages[i] = lt.GetComponent<Image>();
            if (lightImages[i] == null)
                lightImages[i] = lt.GetComponentInChildren<Image>();
            if (lightImages[i] == null)
                Debug.LogWarning($"[LightsOutTask] Light prefab (or its children) has no Image component.");
        }
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private bool ValidatePrefabs()
    {
        if (switchPrefab == null)
        {
            Debug.LogError("[LightsOutTask] switchPrefab is not assigned.");
            return false;
        }
        if (lightPrefab == null)
        {
            Debug.LogError("[LightsOutTask] lightPrefab is not assigned.");
            return false;
        }
        if (switchRowParent == null)
        {
            Debug.LogError("[LightsOutTask] switchRowParent is not assigned.");
            return false;
        }
        if (lightRowParent == null)
        {
            Debug.LogError("[LightsOutTask] lightRowParent is not assigned.");
            return false;
        }
        if (switchCount < 2)
        {
            Debug.LogError("[LightsOutTask] switchCount must be at least 2.");
            return false;
        }
        return true;
    }

    /// <summary>Destroys all children of a transform (immediate in editor, normal in play).</summary>
    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    // -----------------------------------------------------------------------
    //  Puzzle generation
    // -----------------------------------------------------------------------

    private void GeneratePuzzle()
    {
        for (int i = 0; i < switchCount; i++)
            lightState[i] = true;

        Random.State saved = Random.state;
        if (randomSeed >= 0) Random.InitState(randomSeed);

        int steps = Random.Range(minShuffleSteps, maxShuffleSteps + 1);
        int last = -1;

        for (int s = 0; s < steps; s++)
        {
            int cell;
            do { cell = Random.Range(0, switchCount); }
            while (cell == last && switchCount > 1);

            ApplyToggle(cell);
            last = cell;
        }

        // Guard: if shuffle accidentally solved itself, flip the middle switch
        if (IsSolved())
            ApplyToggle(switchCount / 2);

        if (randomSeed >= 0) Random.state = saved;
    }

    // -----------------------------------------------------------------------
    //  Toggle logic
    // -----------------------------------------------------------------------

    private void ApplyToggle(int index)
    {
        FlipCell(index);
        FlipCell(index - 1);
        FlipCell(index + 1);
    }

    private void FlipCell(int index)
    {
        if (index < 0 || index >= switchCount) return;
        lightState[index] = !lightState[index];
    }

    // -----------------------------------------------------------------------
    //  Input
    // -----------------------------------------------------------------------

    private void OnLeverPressed(int index)
    {
        if (!IsOpen || leverAnimating) return;

        PlaySound(leverClickSound);
        ApplyToggle(index);
        RefreshAllVisuals(instant: false);

        if (IsSolved())
            StartCoroutine(SolveSequence());
    }

    // -----------------------------------------------------------------------
    //  Win condition
    // -----------------------------------------------------------------------

    private bool IsSolved()
    {
        for (int i = 0; i < switchCount; i++)
            if (lightState[i]) return false;
        return true;
    }

    private IEnumerator SolveSequence()
    {
        SetButtonsInteractable(false);
        PlaySound(puzzleSolvedSound);
        yield return new WaitForSeconds(0.7f);
        CompleteTask();
    }

    // -----------------------------------------------------------------------
    //  Visuals
    // -----------------------------------------------------------------------

    private void RefreshAllVisuals(bool instant)
    {
        for (int i = 0; i < switchCount; i++)
        {
            bool on = lightState[i];

            if (lightImages[i] != null)
                lightImages[i].color = on ? lightOnColor : lightOffColor;

            if (leverImages[i] != null)
            {
                if (on && leverOnSprite != null) leverImages[i].sprite = leverOnSprite;
                if (!on && leverOffSprite != null) leverImages[i].sprite = leverOffSprite;
            }

            if (leverHandles[i] != null)
            {
                float targetY = on ? leverUpY : leverDownY;

                if (instant)
                {
                    Vector2 ap = leverHandles[i].anchoredPosition;
                    leverHandles[i].anchoredPosition = new Vector2(ap.x, targetY);
                }
                else
                {
                    StartCoroutine(SlideLever(leverHandles[i], targetY));
                }
            }
        }
    }

    private IEnumerator SlideLever(RectTransform handle, float targetY)
    {
        leverAnimating = true;

        float startY = handle.anchoredPosition.y;
        float elapsed = 0f;

        while (elapsed < leverAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / leverAnimDuration);
            Vector2 pos = handle.anchoredPosition;
            handle.anchoredPosition = new Vector2(pos.x, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }

        Vector2 final = handle.anchoredPosition;
        handle.anchoredPosition = new Vector2(final.x, targetY);

        leverAnimating = false;
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var btn in switchButtons)
            if (btn != null) btn.interactable = state;
    }

    // -----------------------------------------------------------------------
    //  Audio
    // -----------------------------------------------------------------------

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}