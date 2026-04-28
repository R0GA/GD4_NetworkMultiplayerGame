using System.Collections.Generic;
using UnityEngine;

public class PropInteractable : MonoBehaviour
{
    public static readonly Dictionary<int, PropInteractable> Registry = new();

    [SerializeField] private string propDisplayName;
    public string DisplayName => string.IsNullOrEmpty(propDisplayName) ? gameObject.name : propDisplayName;

    public int PropIndex { get; private set; }

    private void Awake()
    {
        // Build the full hierarchy path e.g. "Room/Props/Chair"
        // This is identical on every client since it comes from the scene file
        string path = GetHierarchyPath();
        PropIndex = path.GetHashCode();

        if (Registry.ContainsKey(PropIndex))
        {
            Debug.LogError($"[PropInteractable] Hash collision on '{gameObject.name}' (path: {path}). Rename the object to resolve.");
            return;
        }

        Registry.Add(PropIndex, this);
        Debug.Log($"[PropInteractable] Registered '{gameObject.name}' with id {PropIndex}");
    }

    private void OnDestroy()
    {
        Registry.Remove(PropIndex);
    }

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform parent = transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
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