using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attached to each "wire" GameObject inside the task canvas.
/// Draws a thick, dynamic wire between its source socket and either
/// its currently attached destination socket or the user's cursor.
///
/// IMPORTANT BASE CLASS: Must extend MaskableGraphic (not UIBehaviour or MonoBehaviour)
/// so that:
///   1. The Canvas uses its material + mesh pipeline (OnPopulateMesh)
///   2. The EventSystem includes it in raycast hit-testing (IBeginDragHandler works)
///   3. It redraws whenever SetVerticesDirty() is called
/// </summary>
public class WireRenderer : MaskableGraphic,
    IBeginDragHandler, IDragHandler, IEndDragHandler, ICanvasRaycastFilter
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    public int wireIndex = 0;
    public Color wireColor = Color.red;

    [Header("References – set by WireTask.Initialise()")]
    public WireConnector sourceSocket;          // left-side fixed peg
    public WireConnector destinationSocket;     // right-side peg (null while floating)

    [Header("Wire Shape")]
    [SerializeField] private float wireThickness = 18f;
    [SerializeField] private int segmentCount = 32;
    [SerializeField] private float sag = 0.22f;

    [Header("Connector End Cap")]
    [SerializeField] private float connectorLength = 28f;
    [SerializeField] private float connectorWidth = 26f;

    [Header("Drag")]
    [Tooltip("Radius in screen pixels around the source socket that counts as clickable. Increase if dragging feels fussy.")]
    [SerializeField] private float dragHitRadius = 40f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private bool isDragging = false;
    private bool isDetached = false;
    private Vector2 dragCanvasPos;              // cursor in canvas-local space

    // MaskableGraphic: return the wire colour as the graphic's base colour
    public override Color color => wireColor;

    // ── MaskableGraphic – mesh population ────────────────────────────────────
    /// <summary>
    /// Called by the Canvas whenever this graphic needs to rebuild its mesh.
    /// We output a quad-strip bezier wire in the canvas's LOCAL coordinate space.
    /// </summary>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (sourceSocket == null) return;

        // Convert socket world positions → canvas-local positions
        Vector2 start = WorldToCanvasLocal(sourceSocket.WorldPosition);
        Vector2 end;

        if (isDragging)
        {
            end = dragCanvasPos;
        }
        else if (isDetached || destinationSocket == null)
        {
            // Wire droops naturally when unplugged and not being dragged
            end = GetDroopEnd(start);
        }
        else
        {
            end = WorldToCanvasLocal(destinationSocket.WorldPosition);
        }

        BuildQuadStrip(vh, start, end);
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void LateUpdate()
    {
        // Mark dirty every frame so the wire redraws as sockets/cursor move.
        // SetVerticesDirty() is cheap — it just queues a rebuild, not an
        // immediate rebuild — so calling it every frame is acceptable.
        SetVerticesDirty();
    }

    // Make sure the graphic has raycast target ON so drag events fire.
    protected override void Awake()
    {
        base.Awake();
        raycastTarget = true;
    }

    /// <summary>
    /// ICanvasRaycastFilter — only report a valid hit when the pointer is within
    /// dragHitRadius screen pixels of the source socket. This prevents the
    /// full-panel rect of one wire consuming clicks meant for another wire's socket.
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (sourceSocket == null) return false;

        // While dragging, pass events through so OnDrop reaches socket Images below.
        if (isDragging) return false;

        Vector2 socketScreen = sourceSocket.transform.position;
        if (eventCamera != null)
            socketScreen = eventCamera.WorldToScreenPoint(sourceSocket.WorldPosition);

        return Vector2.Distance(screenPoint, socketScreen) <= dragHitRadius;
    }

    // ── Drag Handlers ─────────────────────────────────────────────────────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (sourceSocket == null) return;

        DetachFromDestination();
        isDragging = true;
        isDetached = true;
        dragCanvasPos = ScreenToCanvasLocal(eventData.position);

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        dragCanvasPos = ScreenToCanvasLocal(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        // If the wire wasn't dropped on a socket, it just droops from source.
        // isDetached stays true; the task won't count it as connected.
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void AttachToSocket(WireConnector socket)
    {
        destinationSocket = socket;
        isDetached = false;
        isDragging = false;
    }

    public void DetachFromDestination()
    {
        if (destinationSocket != null)
        {
            destinationSocket.Unplug();
            destinationSocket = null;
        }
        isDetached = true;
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────
    /// <summary>
    /// The canvas this graphic lives in. Walks up the hierarchy once.
    /// </summary>
    private Canvas RootCanvas => canvas; // MaskableGraphic already exposes this

    /// <summary>
    /// Convert a world-space position (e.g. transform.position of a UI socket)
    /// into the local coordinate space of the root Canvas's RectTransform.
    /// This is the space in which OnPopulateMesh vertices must be expressed.
    /// </summary>
    private Vector2 WorldToCanvasLocal(Vector3 worldPos)
    {
        if (RootCanvas == null) return worldPos;
        RectTransform canvasRT = RootCanvas.GetComponent<RectTransform>();

        // ScreenPointToLocalPointInRectangle expects a SCREEN-space point,
        // so convert world → screen first. For Overlay canvases worldCamera
        // is null, but transform.position is already in screen space.
        Camera cam = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : RootCanvas.worldCamera;

        Vector2 screenPoint = cam != null
            ? (Vector2)cam.WorldToScreenPoint(worldPos)
            : (Vector2)worldPos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPoint, cam, out Vector2 local);

        return local;
    }

    /// <summary>
    /// Convert a raw screen-space position (from PointerEventData.position)
    /// into canvas-local space.
    /// </summary>
    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        if (RootCanvas == null) return screenPos;
        RectTransform canvasRT = RootCanvas.GetComponent<RectTransform>();
        Camera cam = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : RootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, cam, out Vector2 local);
        return local;
    }

    // ── Drooping hang position ────────────────────────────────────────────────
    private Vector2 GetDroopEnd(Vector2 start)
    {
        return start + new Vector2(40f, -80f);
    }

    // ── Mesh Generation ───────────────────────────────────────────────────────
    private void BuildQuadStrip(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Color32 c32 = wireColor;

        Vector2 mid = (start + end) * 0.5f;
        float dy = Mathf.Abs(end.y - start.y);
        float dx = Mathf.Abs(end.x - start.x);
        float sagAmt = Mathf.Max(dy, dx) * sag;
        Vector2 ctrl1 = new Vector2(start.x + (end.x - start.x) * 0.35f, mid.y - sagAmt);
        Vector2 ctrl2 = new Vector2(start.x + (end.x - start.x) * 0.65f, mid.y - sagAmt);

        UIVertex vert = UIVertex.simpleVert;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector2 p = CubicBezier(start, ctrl1, ctrl2, end, t);
            Vector2 tangent = CubicBezierTangent(start, ctrl1, ctrl2, end, t).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);

            float taper = 1f - 0.25f * (1f - Mathf.Sin(t * Mathf.PI));
            float halfW = wireThickness * 0.5f * taper;

            int baseIndex = i * 2;

            // Left edge vertex
            vert.position = new Vector3(p.x + normal.x * halfW, p.y + normal.y * halfW, 0f);
            vert.color = c32;
            vert.uv0 = new Vector2(0f, t);
            vh.AddVert(vert);

            // Right edge vertex
            vert.position = new Vector3(p.x - normal.x * halfW, p.y - normal.y * halfW, 0f);
            vert.color = c32;
            vert.uv0 = new Vector2(1f, t);
            vh.AddVert(vert);

            if (i > 0)
            {
                int b = (i - 1) * 2;
                vh.AddTriangle(b, b + 2, b + 1);
                vh.AddTriangle(b + 1, b + 2, b + 3);
            }
        }

        // End-cap connector nub
        AddConnectorCap(vh, end, CubicBezierTangent(start, ctrl1, ctrl2, end, 1f).normalized, c32);
    }

    private void AddConnectorCap(VertexHelper vh, Vector2 tip, Vector2 dir, Color32 c32)
    {
        Vector2 normal = new Vector2(-dir.y, dir.x);
        float hw = connectorWidth * 0.5f;
        Vector2 back = tip - dir * connectorLength;

        UIVertex vert = UIVertex.simpleVert;
        vert.color = c32;
        vert.uv0 = Vector2.zero;

        int b = vh.currentVertCount;

        vert.position = new Vector3(back.x + normal.x * hw, back.y + normal.y * hw, 0f); vh.AddVert(vert);
        vert.position = new Vector3(back.x - normal.x * hw, back.y - normal.y * hw, 0f); vh.AddVert(vert);
        vert.position = new Vector3(tip.x + normal.x * (hw * 0.55f), tip.y + normal.y * (hw * 0.55f), 0f); vh.AddVert(vert);
        vert.position = new Vector3(tip.x - normal.x * (hw * 0.55f), tip.y - normal.y * (hw * 0.55f), 0f); vh.AddVert(vert);

        vh.AddTriangle(b, b + 2, b + 1);
        vh.AddTriangle(b + 1, b + 2, b + 3);
    }

    // ── Bezier Math ───────────────────────────────────────────────────────────
    private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    private static Vector2 CubicBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }
}