using UnityEngine;

public class MoveTowardStimulus : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2 targetPosition;
    public Vector2 TargetPosition => targetPosition;
}
