using UnityEngine;

[DisallowMultipleComponent]
public class PhysicReference : MonoBehaviour
{
    [SerializeField]
    private GameObject targetPhysic;

    public GameObject Get() => targetPhysic;
}
