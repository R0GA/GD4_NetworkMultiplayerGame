using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WireRenderer : MaskableGraphic,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    public int wireIndex = 0;
    public Color wireColor = Color.red;

    [Header("References")]
    public WireConnector sourceSocket;
    public WireConnector destinationSocket;

    [Header("Wire Shape")]
    [SerializeField] private float wireThickness = 18f;
    [SerializeField] private int segmentCount = 32;
    [SerializeField] private float sag = 0.22f;

    [Header("Connector End Cap")]
    [SerializeField] private float connectorLength = 28f;
    [SerializeField] private float connectorWidth = 26f;


    public bool isDragging { get; private set; }
    public bool IsDetached { get; private set; }
    public bool IsTipDragging { get; private set; } // drag started from tip handle
    public Vector2 TipCanvasPosition { get; private set; }

    private Vector2 dragCanvasPos;
    private WireTipHandle tipHandle;

    public override Color color => wireColor;

       protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void SpawnTipHandle(RectTransform tipContainer)
    {
        var go = new GameObject("WireTipHandle", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(tipContainer, false);
        tipHandle = go.AddComponent<WireTipHandle>();
        tipHandle.Initialise(this);
        go.SetActive(false);
    }

    private void LateUpdate()
    {
        if (sourceSocket != null)
        {
            Vector2 start = WorldToCanvasLocal(sourceSocket.WorldPosition);
            TipCanvasPosition = isDragging
                ? dragCanvasPos
                : (IsDetached || destinationSocket == null)
                    ? GetDroopEnd(start)
                    : WorldToCanvasLocal(destinationSocket.WorldPosition);
        }

        if (tipHandle != null)
        {
            // Keep handle active for the full duration of a tip-initiated drag,
            // even though isDragging=true would otherwise hide it.
            bool showHandle = IsDetached && (!isDragging || IsTipDragging);
            tipHandle.UpdateFromWire(showHandle, TipCanvasPosition);
        }

        SetVerticesDirty();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (sourceSocket == null) return;
        DetachFromDestination();
        isDragging = true;
        IsDetached = true;
        dragCanvasPos = ScreenToCanvasLocal(eventData.position);
        transform.SetAsLastSibling();
        if (tipHandle != null) tipHandle.transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        dragCanvasPos = ScreenToCanvasLocal(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        IsTipDragging = false;
        if (IsDetached)
            sourceSocket?.ForceSetPluggedWire(this);
    }


    public void NotifyTipDragBegan() => IsTipDragging = true;
    public void NotifyTipDragEnded() => IsTipDragging = false;

    public void AttachToSocket(WireConnector socket)
    {
        destinationSocket = socket;
        IsDetached = false;
        isDragging = false;
        IsTipDragging = false;
    }

    public void DetachFromDestination()
    {
        if (destinationSocket != null)
        {
            destinationSocket.Unplug();
            destinationSocket = null;
        }
        IsDetached = true;
    }

    private Canvas RootCanvas => canvas;

    private Vector2 WorldToCanvasLocal(Vector3 worldPos)
    {
        if (RootCanvas == null) return worldPos;
        RectTransform canvasRT = RootCanvas.GetComponent<RectTransform>();
        Camera cam = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : RootCanvas.worldCamera;
        Vector2 screenPoint = cam != null
            ? (Vector2)cam.WorldToScreenPoint(worldPos)
            : (Vector2)worldPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPoint, cam, out Vector2 local);
        return local;
    }

    private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
    {
        if (RootCanvas == null) return screenPos;
        RectTransform canvasRT = RootCanvas.GetComponent<RectTransform>();
        Camera cam = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : RootCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT, screenPos, cam, out Vector2 local);
        return local;
    }

    private Vector2 GetDroopEnd(Vector2 start) => start + new Vector2(40f, -80f);

    private void BuildQuadStrip(VertexHelper vh, Vector2 start, Vector2 end)
    {
        Color32 c32 = wireColor;
        Vector2 mid = (start + end) * 0.5f;
        float sagAmt = Mathf.Max(Mathf.Abs(end.y - start.y), Mathf.Abs(end.x - start.x)) * sag;
        Vector2 ctrl1 = new Vector2(start.x + (end.x - start.x) * 0.35f, mid.y - sagAmt);
        Vector2 ctrl2 = new Vector2(start.x + (end.x - start.x) * 0.65f, mid.y - sagAmt);

        UIVertex vert = UIVertex.simpleVert;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector2 p = CubicBezier(start, ctrl1, ctrl2, end, t);
            Vector2 tangent = CubicBezierTangent(start, ctrl1, ctrl2, end, t).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float halfW = wireThickness * 0.5f * (1f - 0.25f * (1f - Mathf.Sin(t * Mathf.PI)));

            vert.color = c32;
            vert.uv0 = new Vector2(0f, t);
            vert.position = new Vector3(p.x + normal.x * halfW, p.y + normal.y * halfW, 0f);
            vh.AddVert(vert);

            vert.uv0 = new Vector2(1f, t);
            vert.position = new Vector3(p.x - normal.x * halfW, p.y - normal.y * halfW, 0f);
            vh.AddVert(vert);

            if (i > 0)
            {
                int b = (i - 1) * 2;
                vh.AddTriangle(b, b + 2, b + 1);
                vh.AddTriangle(b + 1, b + 2, b + 3);
            }
        }

        AddConnectorCap(vh, end,
            CubicBezierTangent(start, ctrl1, ctrl2, end, 1f).normalized, c32);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (sourceSocket == null) return;
        Vector2 start = WorldToCanvasLocal(sourceSocket.WorldPosition);
        BuildQuadStrip(vh, start, TipCanvasPosition == Vector2.zero
            ? GetDroopEnd(start) : TipCanvasPosition);
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
