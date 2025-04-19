using UnityEngine;

[CreateAssetMenu(fileName = " Property", menuName = "Scriptable Objects/Item/Consummable")]
public class ConsummableProperty : ItemProperty
{
    [Header("Consummable Settings")]
    [SerializeField]
    private int healAmount;
    public int HealAmount => healAmount;
}
