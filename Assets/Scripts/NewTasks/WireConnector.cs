using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Image))]
public class WireConnector : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    public Color socketColor = Color.white;
    public bool isSource = true;
    public int correctWireIndex = 0;

    [Header("Visuals")]
    [SerializeField] private Image socketImage;
    [SerializeField] private Image highlightRing;

    public WireRenderer pluggedWire { get; private set; }

    public WireRenderer activeDragWire { get; private set; }

    private static readonly Color kHighlightColor = new Color(1f, 1f, 0.3f, 0.85f);

    private void Awake()
    {
        if (socketImage == null) socketImage = GetComponent<Image>();
        if (socketImage) socketImage.raycastTarget = true;
        if (highlightRing) highlightRing.enabled = false;
        
    }

    void Start()
    {
        socketImage.color = socketColor;
    }

    public void OnPointerDown(PointerEventData eventData) { }


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (pluggedWire == null) return;

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

    public void OnDrop(PointerEventData eventData)
    {

        WireRenderer dragged = null;

        // Case 1: drag source is a WireConnector forwarding its wire
        WireConnector srcSocket = eventData.pointerDrag?.GetComponent<WireConnector>();
        if (srcSocket != null)
            dragged = srcSocket.activeDragWire;

        // Case 2: drag source is a WireTipHandle — ask it for its wire
        if (dragged == null)
            dragged = eventData.pointerDrag?.GetComponent<WireTipHandle>()?.GetDragWire();

        // Case 3: drag source is a WireRenderer directly
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        bool wireIsDragging =
            eventData.pointerDrag.GetComponent<WireRenderer>() != null ||
            eventData.pointerDrag.GetComponent<WireConnector>()?.activeDragWire != null ||
            eventData.pointerDrag.GetComponent<WireTipHandle>()?.GetDragWire() != null;

        if (wireIsDragging)
            SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);

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