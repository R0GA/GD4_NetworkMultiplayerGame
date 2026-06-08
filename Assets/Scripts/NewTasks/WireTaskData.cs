using UnityEngine;

[CreateAssetMenu(fileName = "WireTaskData", menuName = "WireTask/Wire Task Data")]
public class WireTaskData : ScriptableObject
{
    [Tooltip("One entry per wire. The order here defines the CORRECT terminal order (top → bottom on the right side).")]
    public WireDefinition[] wires = new WireDefinition[]
    {
        new WireDefinition { color = Color.red,    label = "Red"    },
        new WireDefinition { color = Color.yellow,  label = "Yellow" },
        new WireDefinition { color = Color.cyan,    label = "Blue"   },
        new WireDefinition { color = Color.green,   label = "Green"  },
    };
}

[System.Serializable]
public class WireDefinition
{
    public Color color  = Color.white;
    public string label = "";
}
