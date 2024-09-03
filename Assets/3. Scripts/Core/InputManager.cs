using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Main;
    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        inputActions = new DefaultInputActions();
    }

    private DefaultInputActions inputActions;

    public DefaultInputActions InputActions => inputActions;

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
}
