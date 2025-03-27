using UnityEngine;

public class MoveableController : MonoBehaviour
{
    private IMoveable[] moveables;

    private void Awake()
    {
        moveables = GetComponentsInChildren<IMoveable>();
    }

    public void SetMoveable(bool value)
    {
        foreach (var moveable in moveables)
        {
            moveable.SetMoveable(value);
        }
    }
}
