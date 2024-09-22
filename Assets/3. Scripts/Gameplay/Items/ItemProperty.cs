using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject
{
    [Header("Item Settings")]
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private bool isConsummable;
    [SerializeField]
    private int maxStack = 99;
    [SerializeField]
    private float primaryCdr = 0.25f;
    [SerializeField]
    private float secondaryCdr = 0.25f;


    public Sprite Sprite => sprite;
    public int MaxStack => maxStack;
    public bool IsConsummable => isConsummable;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;

    public virtual bool OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {
        return false;
    }

    public virtual bool OnSecondaryAction(Vector2 position)
    {
        return false;
    }

    public virtual bool OnAlternativeAction(Vector2 position)
    {
        return false;
    }
}
