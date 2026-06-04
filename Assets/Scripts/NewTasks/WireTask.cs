using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Circuit-Breaker Wire-Swap Task.
///
/// KEY FIXES vs original:
///  1. Wire prefab RectTransform is stretched to fill the whole wireContainer
///     so the WireRenderer (MaskableGraphic) occupies the full panel area and
///     can receive drag events anywhere on it.
///  2. WireContainer sits ABOVE socket containers in the hierarchy so wires
///     are drawn on top, but its CanvasGroup has Blocks Raycasts = FALSE so
///     clicks pass through to sockets underneath for OnDrop.
///  3. BuildTask() properly sizes each wire's RectTransform.
/// </summary>
public class WireTask : BaseTask
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Wire Task – Data")]
    [SerializeField] private WireTaskData taskData;

    [Header("Wire Task – Prefabs")]
    [Tooltip("Prefab with a WireRenderer (MaskableGraphic) component. One spawned per wire.")]
    [SerializeField] private GameObject wirePrefab;

    [Tooltip("Prefab with an Image + WireConnector component. Spawned for source AND destination sockets.")]
    [SerializeField] private GameObject socketPrefab;

    [Header("Wire Task – Layout Parents")]
    [Tooltip("RectTransform that is parent for all wire GameObjects. Must have CanvasGroup.BlocksRaycasts = false.")]
    [SerializeField] private RectTransform wireContainer;

    [Tooltip("Parent for LEFT (source) sockets.")]
    [SerializeField] private RectTransform leftSocketContainer;

    [Tooltip("Parent for RIGHT (destination) sockets.")]
    [SerializeField] private RectTransform rightSocketContainer;

    [Header("Wire Task – UI")]
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Button closeButton;

    [Tooltip("Flash this overlay green briefly when the task completes.")]
    [SerializeField] private Image completionFlash;

    [Header("Wire Task – Layout")]
    [SerializeField] private float socketSpacing = 90f;
    [SerializeField] private bool  shuffleDestinationsOnOpen = true;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private WireConnector[] sourceSockets;
    private WireConnector[] destSockets;
    private WireRenderer[]  wires;

    private bool taskInitialised = false;

    // ── BaseTask overrides ────────────────────────────────────────────────────
    protected override void OnOpen()
    {
        if (!taskInitialised)
        {
            BuildTask();
            taskInitialised = true;
        }
        else
        {
            ResetWires();
        }

        if (statusLabel)      statusLabel.text = "Rewire the breaker panel.";
        if (completionFlash)  completionFlash.color = Color.clear;

        if (closeButton)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseTask);
        }
    }

    protected override void OnClose() { }

    // ── Build ─────────────────────────────────────────────────────────────────
    private void BuildTask()
    {
        if (taskData == null)
        {
            Debug.LogError("[WireTask] taskData is not assigned!", this);
            return;
        }

        int count = taskData.wires.Length;
        sourceSockets = new WireConnector[count];
        destSockets   = new WireConnector[count];
        wires         = new WireRenderer[count];

        // Ensure wireContainer blocks no raycasts (wires draw on top, but
        // OnDrop events must reach the socket Images below).
        EnsureCanvasGroupPassthrough(wireContainer.gameObject);

        int[] shuffled = shuffleDestinationsOnOpen ? ShuffledIndices(count) : IdentityIndices(count);

        float startY = socketSpacing * (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            WireDefinition def = taskData.wires[i];

            // ── Left source socket ─────────────────────────────────────────────
            WireConnector src = SpawnSocket(leftSocketContainer, def, i, true,
                new Vector2(0f, startY - i * socketSpacing));
            sourceSockets[i] = src;

            // ── Right destination socket (shuffled row) ────────────────────────
            int destSlot = shuffled[i];
            WireConnector dst = SpawnSocket(rightSocketContainer, def, i, false,
                new Vector2(0f, startY - destSlot * socketSpacing));
            destSockets[i] = dst;

            // ── Wire ──────────────────────────────────────────────────────────
            GameObject wireGO = Instantiate(wirePrefab, wireContainer);
            wireGO.name = $"Wire_{def.label}";

            // CRITICAL: stretch wire RectTransform to fill wireContainer so
            // the MaskableGraphic covers the whole panel and drag events can
            // originate anywhere near the source socket.
            RectTransform wireRT = wireGO.GetComponent<RectTransform>();
            wireRT.anchorMin        = Vector2.zero;
            wireRT.anchorMax        = Vector2.one;
            wireRT.offsetMin        = Vector2.zero;
            wireRT.offsetMax        = Vector2.zero;
            wireRT.anchoredPosition = Vector2.zero;

            WireRenderer wr = wireGO.GetComponent<WireRenderer>();
            if (wr == null)
            {
                Debug.LogError("[WireTask] Wire prefab is missing a WireRenderer component!", wireGO);
                continue;
            }

            wr.wireIndex    = i;
            wr.wireColor    = def.color;
            wr.sourceSocket = src;
            wires[i]        = wr;
        }

        ResetWires();
    }

    private WireConnector SpawnSocket(RectTransform container, WireDefinition def,
                                      int index, bool isSource, Vector2 anchoredPos)
    {
        GameObject go = Instantiate(socketPrefab, container);
        go.name = isSource ? $"SourceSocket_{def.label}" : $"DestSocket_{def.label}";

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(50f, 50f);
        rt.anchoredPosition = anchoredPos;

        WireConnector conn = go.GetComponent<WireConnector>();
        if (conn == null)
        {
            Debug.LogError("[WireTask] Socket prefab is missing a WireConnector component!", go);
            return null;
        }

        conn.socketColor      = def.color;
        conn.isSource         = isSource;
        conn.correctWireIndex = index;

        AddSocketLabel(go.transform, def.label, def.color);
        return conn;
    }

    private void AddSocketLabel(Transform parent, string text, Color col)
    {
        var go  = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, -36f);
        rt.sizeDelta        = new Vector2(80f, 24f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = col;
        tmp.fontSize  = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// Plugs every wire back into its own colour-matching destination socket
    /// (the starting "correct" state the player must undo).
    /// </summary>
    private void ResetWires()
    {
        if (wires == null) return;
        for (int i = 0; i < wires.Length; i++)
        {
            wires[i].DetachFromDestination();
            destSockets[i].Unplug();
            destSockets[i].ForceSetPluggedWire(wires[i]);
            wires[i].AttachToSocket(destSockets[i]);
        }
    }

    // ── Per-Frame Completion Check ────────────────────────────────────────────
    private void Update()
    {
        if (!IsOpen || IsComplete) return;
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (wires == null || destSockets == null) return;

        foreach (WireConnector dest in destSockets)
        {
            WireRenderer plugged = dest.pluggedWire;
            if (plugged == null)                            return; // socket empty
            if (plugged.wireIndex == dest.correctWireIndex) return; // still matched
        }

        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        if (statusLabel) statusLabel.text = "Wiring sabotaged!";

        if (completionFlash)
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                completionFlash.color = new Color(0.1f, 1f, 0.4f, Mathf.PingPong(t * 4f, 0.6f));
                yield return null;
            }
            completionFlash.color = Color.clear;
        }

        yield return new WaitForSeconds(0.25f);
        CompleteTask();
    }

    // ── Utility ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Adds a CanvasGroup to the given GameObject (if missing) and sets
    /// Blocks Raycasts to false so pointer events pass through to sockets.
    /// </summary>
    private static void EnsureCanvasGroupPassthrough(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable   = true;
        cg.alpha          = 1f;
    }

    private static int[] ShuffledIndices(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;

        // Fisher-Yates
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }

        // Guarantee no fixed points (derangement)
        bool hasFix = true;
        int  guard  = 0;
        while (hasFix && guard++ < 50)
        {
            hasFix = false;
            for (int i = 0; i < count; i++)
            {
                if (arr[i] != i) continue;
                hasFix = true;
                int k = (i + 1 + Random.Range(0, count - 1)) % count;
                (arr[i], arr[k]) = (arr[k], arr[i]);
                break;
            }
        }
        return arr;
    }

    private static int[] IdentityIndices(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        return arr;
    }
}
