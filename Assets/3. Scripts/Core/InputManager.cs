using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum InputMap
{
    Gameplay,
    UI,
    Console
}

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
        gameplayActionMap = inputActions.Gameplay;
        consoleActionMap = inputActions.Console;
        if (SceneManager.GetActiveScene().name == mainMenuScene)
            SwitchMap(InputMap.UI);
        else
            SwitchMap(InputMap.Gameplay);
    }

    [Header("Settings")]
    [SerializeField]
    private string mainGameScene = "Main Game";
    [SerializeField]
    private string mainMenuScene = "Main Menu";

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;

    private DefaultInputActions inputActions;
    public DefaultInputActions InputActions => inputActions;

    private InputActionMap gameplayActionMap;
    private InputActionMap uiActionMap;
    private InputActionMap consoleActionMap;

    private InputActionMap previousActionMap;
    private InputActionMap currentActionMap;

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public void SwitchMap(InputMap map)
    {
        if (currentActionMap != null)
        {
            currentActionMap.Disable();
            previousActionMap = currentActionMap;
        }

        currentActionMap = GetActionMap(map);
    }

    public void SwitchToPreviousMap()
    {
        if (previousActionMap != null)
        {
            currentActionMap.Disable();
            var temp = currentActionMap;
            currentActionMap = previousActionMap;
            previousActionMap = temp;
        }
    }

    private InputActionMap GetActionMap(InputMap map)
    {
        switch (map)
        {
            case InputMap.Gameplay:
                if (showDebugs) Debug.Log("Switched to Gameplay Map");
                return gameplayActionMap;
            case InputMap.Console:
                if (showDebugs) Debug.Log("Switched to Console Map");
                return consoleActionMap;
            case InputMap.UI:
                if (showDebugs) Debug.Log("Switched to UI Map");
                return uiActionMap;
            default:
                Debug.LogError("Invalid InputMap");
                return null;
        }
    }
}
