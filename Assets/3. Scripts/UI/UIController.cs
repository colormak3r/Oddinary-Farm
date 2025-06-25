using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour, DefaultInputActions.IUIActions
{
    public static UIController Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

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

    public void OnNavigate(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnCancel(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (ShopUI.Main && ShopUI.Main.IsShowing)
            {
                ShopUI.Main.CloseShop();
            }
            else if (OptionsUI.Main && OptionsUI.Main.IsShowing)
            {
                OptionsUI.Main.ResumeButtonClicked();
            }
            else if (StatUI.Main && StatUI.Main.IsShowing)
            {
                StatUI.Main.CloseButton();
            }
        }
    }

    public void OnSubmit(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnPoint(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnRightClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnMiddleClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnScrollWheel(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnTrackedDevicePosition(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }

    public void OnTrackedDeviceOrientation(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {

    }
}
