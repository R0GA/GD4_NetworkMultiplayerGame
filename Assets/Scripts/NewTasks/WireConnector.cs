using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Placed on each terminal socket (both left source sockets and right destination sockets).
/// Tracks which WireRenderer is currently plugged in, and handles drop events.
/// Also forwards drag events to the plugged wire so the wire follows the cursor.
/// </summary>
[RequireComponent(typeof(Image))]
public class WireConnector : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    public Color socketColor = Color.white;
    public bool isSource = true;
    public int correctWireIndex = 0;

    [Header("Visuals")]
    [SerializeField] private Image socketImage;
    [SerializeField] private Image highlightRing;

    // ── Runtime ───────────────────────────────────────────────────────────────
    public WireRenderer pluggedWire { get; private set; }

    // Cached during OnBeginDrag so OnDrag/OnEndDrag still have a reference
    // even after pluggedWire is cleared by DetachFromDestination().
    private WireRenderer activeDragWire;

    private static readonly Color kHighlightColor = new Color(1f, 1f, 0.3f, 0.85f);

    private void Awake()
    {
        if (socketImage == null) socketImage = GetComponent<Image>();
        if (socketImage) socketImage.color = socketColor;
        if (socketImage) socketImage.raycastTarget = true;
        if (highlightRing) highlightRing.enabled = false;
    }

    // ── IPointerDownHandler ───────────────────────────────────────────────────
    // Empty but REQUIRED — without this the EventSystem never tracks this object
    // for drag events, so OnBeginDrag/OnDrag/OnEndDrag/OnDrop are all ignored.
    public void OnPointerDown(PointerEventData eventData) { }

    // ── Drag forwarding ───────────────────────────────────────────────────────
    // Raycasts hit the socket Image rather than the WireRenderer graphic, so
    // the socket must forward all drag events to whichever wire it holds.

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (pluggedWire == null) return;

        // Cache the wire BEFORE forwarding — OnBeginDrag calls DetachFromDestination
        // which sets pluggedWire = null on this socket, so we'd lose the reference.
        activeDragWire = pluggedWire;

        ExecuteEvents.Execute(activeDragWire.gameObject, eventData,
            ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeDragWire == null) return;
        ExecuteEvents.Execute(activeDragWire.gameObject, eventData,
            ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (activeDragWire == null) return;
        ExecuteEvents.Execute(activeDragWire.gameObject, eventData,
            ExecuteEvents.endDragHandler);
        activeDragWire = null;
    }

    // ── IDropHandler ──────────────────────────────────────────────────────────
    public void OnDrop(PointerEventData eventData)
    {
        // pointerDrag is the socket that started the drag, not the wire.
        // Retrieve the wire via the socket's activeDragWire.
        WireRenderer dragged = null;

        // First try: the drag source is a WireConnector forwarding its wire
        WireConnector srcSocket = eventData.pointerDrag?.GetComponent<WireConnector>();
        if (srcSocket != null)
            dragged = srcSocket.activeDragWire;

        // Fallback: drag source is a WireRenderer directly
        if (dragged == null)
            dragged = eventData.pointerDrag?.GetComponent<WireRenderer>();

        if (dragged == null) return;

        // Source sockets only accept their own wire back
        if (isSource && dragged.wireIndex != correctWireIndex) return;

        // Destination sockets only accept one wire at a time
        if (!isSource && pluggedWire != null && pluggedWire != dragged) return;

        dragged.DetachFromDestination();

        pluggedWire = dragged;
        dragged.AttachToSocket(this);

        SetHighlight(false);
    }

    // ── IPointerEnterHandler / Exit ───────────────────────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight when any wire drag is in progress.
        // pointerDrag may be a WireConnector (forwarding) or WireRenderer (direct).
        if (eventData.pointerDrag == null) return;

        bool wireIsDragging =
            eventData.pointerDrag.GetComponent<WireRenderer>() != null ||
            eventData.pointerDrag.GetComponent<WireConnector>()?.activeDragWire != null;

        if (wireIsDragging)
            SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

    // ── Public API ────────────────────────────────────────────────────────────
    public void Unplug() => pluggedWire = null;
    public void ForceSetPluggedWire(WireRenderer wire) => pluggedWire = wire;

    public void SetHighlight(bool on)
    {
        if (highlightRing)
        {
            highlightRing.enabled = on;
            highlightRing.color = kHighlightColor;
        }
    }

    public Vector3 WorldPosition => transform.position;
}