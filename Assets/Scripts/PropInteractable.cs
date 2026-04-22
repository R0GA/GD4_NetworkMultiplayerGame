using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to any scene GameObject that players can disguise themselves as.
/// Automatically registers/unregisters itself in the global prop registry.
/// </summary>
public class PropInteractable : MonoBehaviour
{
    /// <summary>
    /// Global registry of every active PropInteractable in the scene.
    /// Indexed by PropIndex — used by SlugPlayer to sync transforms over the network.
    /// </summary>
    public static readonly List<PropInteractable> Registry = new();

    /// <summary>This prop's stable index into <see cref="Registry"/>.</summary>
    public int PropIndex { get; private set; }

    [Tooltip("Optional: override the display name shown in UI hints. Defaults to the GameObject name.")]
    [SerializeField] private string propDisplayName;
    public string DisplayName => string.IsNullOrEmpty(propDisplayName) ? gameObject.name : propDisplayName;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        PropIndex = Registry.Count;
        Registry.Add(this);
    }

    private void OnDestroy()
    {
        Registry.Remove(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
    }
#endif
}
