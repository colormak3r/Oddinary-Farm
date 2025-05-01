using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour, DefaultInputActions.IUIActions
{
    private void Start()
    {
        InputManager.Main.InputActions.UI.SetCallbacks(this);
    }

    public void OnOpenConsole(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ConsoleUI.Main.OpenConsole();
        }
    }

    public void OnBack(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // TODO: Implement concrete method of UI back action
    }

    public void OnNavigate(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        
    }

    public void OnClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        
    }

    public void OnCancel(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        
    }
}
