using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class WireTipHandle : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private WireRenderer wire;
    private RectTransform rt;
    private Image img;

    public void Initialise(WireRenderer owner)
    {
        wire = owner;
        rt = GetComponent<RectTransform>();

        img = GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = true;

        var cg = GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 1f;
        cg.ignoreParentGroups = true;

        rt.sizeDelta = new Vector2(56f, 56f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        gameObject.SetActive(false);
    }

    /// <summary>Returns the wire this handle belongs to, for use by WireConnector.OnDrop.</summary>
    public WireRenderer GetDragWire() => wire;

    /// <summary>Called every frame by WireRenderer.LateUpdate (which always runs).</summary>
    public void UpdateFromWire(bool shouldBeActive, Vector2 tipPos)
    {
        if (gameObject.activeSelf != shouldBeActive)
            gameObject.SetActive(shouldBeActive);

        if (shouldBeActive)
            rt.anchoredPosition = tipPos;
    }

    // ── Input ─────────────────────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (wire == null) return;

        // Stop the tip handle intercepting raycasts while it follows the cursor.
        // Without this it sits on top of every socket the player hovers over,
        // blocking OnPointerEnter and OnDrop from ever reaching the socket Image.
        if (img != null) img.raycastTarget = false;

        wire.NotifyTipDragBegan();
        ExecuteEvents.Execute(wire.gameObject, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (wire == null) return;
        ExecuteEvents.Execute(wire.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (wire == null) return;

        // Re-enable so the player can grab the handle again next time.
        if (img != null) img.raycastTarget = true;

        wire.NotifyTipDragEnded();
        ExecuteEvents.Execute(wire.gameObject, eventData, ExecuteEvents.endDragHandler);
    }
}