using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private void Test()
    {
        Selector.main.Select(Position, Size);
    }
}
