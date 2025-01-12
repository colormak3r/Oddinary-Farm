using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hammer Property", menuName = "Scriptable Objects/Item/Hammer")]
public class HammerProperty : ToolProperty
{
    [Header("Hammer Settings")]
    [SerializeField]
    private GameObject[] structures;
    [SerializeField]
    private LayerMask structureLayers;

    [Header("Hammer Preview Settings")]
    [SerializeField]
    private Sprite fixIconSprite;
    [SerializeField]
    private Sprite moveIconSprite;
    [SerializeField]
    private Sprite removeIconSprite;

    public GameObject[] Structures => structures;
    public LayerMask StructureLayers => structureLayers;
    public Sprite FixIconSprite => fixIconSprite;
    public Sprite MoveIconSprite => moveIconSprite;
    public Sprite RemoveIconSprite => removeIconSprite;
}
