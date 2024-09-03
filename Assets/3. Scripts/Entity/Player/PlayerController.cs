using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, DefaultInputActions.IPlayerActions
{
    private EntityMovement movement;

    private void Awake()
    {
        movement = GetComponent<EntityMovement>();        
    }

    private void Start()
    {
        InputManager.Main.InputActions.Player.SetCallbacks(this);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var direction = context.ReadValue<Vector2>().normalized;
        movement.SetDirection(direction);
    }
}
