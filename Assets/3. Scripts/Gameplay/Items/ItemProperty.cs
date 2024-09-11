using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Item(Test Only)")]
public class ItemProperty : ScriptableObject
{
    [Header("Item Settings")]
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private int maxStack = 99;
    [SerializeField]
    private float primaryCdr = 0.25f;
    [SerializeField]
    private float secondaryCdr = 0.25f;
    
    public Sprite Sprite => sprite;
    public int MaxStack => maxStack;
    public float PrimaryCdr => primaryCdr;
    public float SecondaryCdr => secondaryCdr;

    public virtual void OnPrimaryAction(Vector2 position, PlayerInventory inventory)
    {

    }

    public virtual void OnSecondaryAction(Vector2 position)
    {

    }
    public virtual void OnAlternativeAction(Vector2 position)
    {

    }
}
