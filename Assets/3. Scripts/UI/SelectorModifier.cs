using UnityEngine;

[ExecuteAlways]
public class SelectorModifier : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    private Vector2 size = new Vector2(2, 2);

    public Vector2 Position => (Vector2)transform.position + offset;
    public Vector2 Size => size;

    [ContextMenu("Test")]
    public void Test()
    {
        Selector.Main.Test(Position, Size);
    }

    [ContextMenu("Reset")]
    public void Reset()
    {
        Selector.Main.Reset();
    }
}
