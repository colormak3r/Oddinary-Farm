/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/09/2025
 * Last Modified:   07/25/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public enum PetType
{
    Chihuahua,
    Hamster,
}

[CreateAssetMenu(fileName = " PetData", menuName = "Scriptable Objects/Pet Data")]
public class PetData : ScriptableObject
{
    [SerializeField]
    public PetType petType;
    [SerializeField]
    public Sprite petIcon;
    [SerializeField]
    public Sprite[] collectedSprites;
    [SerializeField]
    public Sprite[] hiddenSprites;
    [SerializeField]
    public GameObject petPrefab;
    [SerializeField]
    [TextArea(2, 3)]
    public string petHint;
    [SerializeField]
    [TextArea(2, 3)]
    public string petDescription;
}
