using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class RaycastDebugger : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var results = new List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
        {
            Debug.Log("[RaycastDebugger] Nothing hit!");
            return;
        }

        foreach (var r in results)
            Debug.Log($"[RaycastDebugger] Hit: '{r.gameObject.name}' on '{r.gameObject.transform.parent?.name}' | depth {r.depth}");
    }
}