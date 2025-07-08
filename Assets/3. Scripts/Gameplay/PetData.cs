using UnityEngine;

public enum PetType
{
    Chihuahua,
}

[CreateAssetMenu(fileName = " PetData", menuName = "Scriptable Objects/Pet Data")]
public class PetData : ScriptableObject
{
    [SerializeField]
    public PetType petType;
    [SerializeField]
    public Sprite petIcon;
    [SerializeField]
    public GameObject petPrefab;
}
