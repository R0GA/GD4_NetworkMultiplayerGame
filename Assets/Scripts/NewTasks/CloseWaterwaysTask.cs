using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Close Waterways minigame task.
/// The player clicks and drags the valve wheel counter-clockwise to reduce
/// water flow from 100% down to 0%.  Extends BaseTask so it plugs straight
/// into your existing TaskManager / TaskTrigger system.
///
/// Inspector quick-reference
/// ─────────────────────────
///  taskPanel          ► The root Canvas / panel GameObject for this task UI
///  taskDisplayName    ► "Close Waterways"   (shown in task list HUD)
///  taskIdentifier     ► e.g. "close_waterways"  (must match TaskTrigger)
///  valveTransform     ► The RectTransform of the valve wheel Image
///  flowFillImage      ► Image (fill type = Filled, method = Vertical) for water meter
///  flowBarFill        ► Optionally a second image for animated fill colour
///  completionMessage  ► Optional GameObject shown briefly on success
///  turnsRequired      ► How many full 360° turns to fully close valve (default 2)
///  dragSensitivity    ► Mouse-drag speed multiplier (default 1.0)
/// </summary>
public class CloseWaterwaysTask : BaseTask,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    // ── Inspector fields ───────────────────────────────────────────────────

    [Header("Valve")]
    [Tooltip("RectTransform of the valve wheel sprite that will visually rotate.")]
    [SerializeField] private RectTransform valveTransform;

    [Tooltip("How many full counter-clockwise rotations are needed to close the valve.")]
    [SerializeField] private float turnsRequired = 2f;

    [Tooltip("Multiplier applied to mouse-drag delta before converting to rotation.")]
    [SerializeField] private float dragSensitivity = 1f;

    [Tooltip("Visual feedback: shakes/wobbles the valve when dragged the wrong way.")]
    [SerializeField] private bool wrongDirectionFeedback = true;

    [Header("Water Flow Meter")]
    [Tooltip("UI Image with Fill Method = Vertical.  fillAmount drives the water level.")]
    [SerializeField] private Image flowFillImage;

    [Tooltip("Optional gradient colours applied to the flow bar (full=blue, empty=grey).")]
    [SerializeField] private Gradient flowBarGradient;

    [Header("Completion")]
    [Tooltip("Optional GameObject displayed briefly when the task is completed.")]
    [SerializeField] private GameObject completionMessage;

    [Tooltip("Seconds the completion message is shown before the panel closes.")]
    [SerializeField] private float completionDelay = 1.2f;

    // ── Runtime state ──────────────────────────────────────────────────────

    // Total CCW degrees accumulated by the player.
    private float totalCCWDegrees = 0f;

    // Degrees needed = full turns × 360.
    private float DegreesRequired => turnsRequired * 360f;

    // Flow is 1 → 0  (1 = fully open, 0 = fully closed).
    private float FlowNormalized => Mathf.Clamp01(1f - (totalCCWDegrees / DegreesRequired));

    // Drag tracking
    private bool isDragging = false;
    private Vector2 lastMousePos;
    private Vector2 valveCenterScreen; // centre of the valve in screen space

    // Visual rotation (purely cosmetic accumulator, unbounded).
    private float visualRotationDeg = 0f;

    // Wobble coroutine handle
    private Coroutine wobbleCoroutine;

    // ── BaseTask overrides ─────────────────────────────────────────────────

    protected override void OnOpen()
    {
        // Reset state each time the panel opens
        // (keeps prior progress if you want persistence — remove the line below
        //  if you want players to have to finish in one sitting.)
        // totalCCWDegrees = 0f;

        UpdateVisuals();
    }

    protected override void OnClose()
    {
        isDragging = false;
    }

    // ── Drag interaction ───────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsOpen || IsComplete) return;

        isDragging = true;
        lastMousePos = eventData.position;

        // Cache the valve's screen-space centre for angle calculations.
        valveCenterScreen = GetValveCentreScreenPos();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !IsOpen || IsComplete) return;

        Vector2 currentMousePos = eventData.position;

        // Vectors from valve centre to previous and current mouse positions.
        Vector2 fromVec = lastMousePos - valveCenterScreen;
        Vector2 toVec = currentMousePos - valveCenterScreen;

        // Signed angle: positive = CCW in screen space.
        float signedAngle = Vector2.SignedAngle(fromVec, toVec);

        if (signedAngle > 0f)
        {
            // Counter-clockwise (correct direction) — accumulate progress.
            totalCCWDegrees += signedAngle;
            totalCCWDegrees = Mathf.Min(totalCCWDegrees, DegreesRequired);
            visualRotationDeg += signedAngle;
        }
        else if (signedAngle < 0f && wrongDirectionFeedback)
        {
            // Clockwise (wrong direction) — play wobble feedback but DON'T
            // subtract progress (feel free to change this design decision).
            TriggerWobble();
        }

        lastMousePos = currentMousePos;

        UpdateVisuals();

        // Check win condition
        if (totalCCWDegrees >= DegreesRequired)
        {
            StartCoroutine(CompleteWithDelay());
        }
    }

    // ── Visual helpers ─────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        // Rotate the wheel sprite
        if (valveTransform != null)
            valveTransform.localRotation = Quaternion.Euler(0f, 0f, visualRotationDeg);

        // Update flow bar fill amount
        if (flowFillImage != null)
            flowFillImage.fillAmount = FlowNormalized;

        // Tint the bar if a gradient is supplied
        if (flowFillImage != null && flowBarGradient != null)
            flowFillImage.color = flowBarGradient.Evaluate(FlowNormalized);
    }

    private Vector2 GetValveCentreScreenPos()
    {
        if (valveTransform == null) return Vector2.zero;

        Canvas canvas = valveTransform.GetComponentInParent<Canvas>();
        if (canvas == null) return Vector2.zero;

        // Works for both Screen Space - Overlay and Screen Space - Camera canvases.
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            valveTransform.parent as RectTransform,
            Vector2.zero, cam, out _);

        // Convert valve's anchored centre to screen space.
        Vector3[] corners = new Vector3[4];
        valveTransform.GetWorldCorners(corners);
        Vector3 worldCentre = (corners[0] + corners[2]) * 0.5f;

        if (cam != null)
            return cam.WorldToScreenPoint(worldCentre);

        return RectTransformUtility.WorldToScreenPoint(null, worldCentre);
    }

    // ── Completion sequence ────────────────────────────────────────────────

    private System.Collections.IEnumerator CompleteWithDelay()
    {
        isDragging = false;

        if (completionMessage != null)
            completionMessage.SetActive(true);

        yield return new WaitForSeconds(completionDelay);

        if (completionMessage != null)
            completionMessage.SetActive(false);

        CompleteTask(); // inherited from BaseTask — notifies TaskManager
    }

    // ── Wrong-direction wobble ─────────────────────────────────────────────

    private void TriggerWobble()
    {
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(WobbleRoutine());
    }

    private System.Collections.IEnumerator WobbleRoutine()
    {
        if (valveTransform == null) yield break;

        float elapsed = 0f;
        float duration = 0.25f;
        float magnitude = 4f;
        float baseRot = visualRotationDeg;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Sin(elapsed * 60f) * magnitude * (1f - elapsed / duration);
            valveTransform.localRotation = Quaternion.Euler(0f, 0f, baseRot + shake);
            yield return null;
        }

        // Restore clean rotation
        valveTransform.localRotation = Quaternion.Euler(0f, 0f, visualRotationDeg);
    }
}