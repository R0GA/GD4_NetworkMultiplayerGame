using System.Collections.Generic;
using UnityEngine;

public class PropInteractable : MonoBehaviour
{
    public static readonly Dictionary<int, PropInteractable> Registry = new();

    // Static data survives when domain reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Registry.Clear();

    [SerializeField] private string propDisplayName;
    public string DisplayName => string.IsNullOrEmpty(propDisplayName) ? gameObject.name : propDisplayName;

    public int PropIndex { get; private set; }

    private void Awake()
    {
        string path = GetHierarchyPath();
        PropIndex = StableHash(path);

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
        // Only remove our own entry (a collision loser must not evict the winner).
        if (Registry.TryGetValue(PropIndex, out var existing) && existing == this)
            Registry.Remove(PropIndex);
    }

    // string.GetHashCode() is not guaranteed to match between processes/platforms,
    // and this id is sent over the network, so use a fixed FNV-1a hash instead.
    private static int StableHash(string s)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char c in s) { h ^= c; h *= 16777619; }
            return (int)h;
        }
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