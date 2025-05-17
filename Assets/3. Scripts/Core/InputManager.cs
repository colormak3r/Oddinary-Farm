using System;
using Unity.Netcode;
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

        DontDestroyOnLoad(gameObject);

        inputActions = new DefaultInputActions();
        gameplayActionMap = inputActions.Gameplay;
        consoleActionMap = inputActions.Console;
        uiActionMap = inputActions.UI;
    }

    //[Header("Settings")]

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private InputMap currentInputMap;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    private void OnDisable()
    {
        inputActions?.Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            SwitchMap(InputMap.UI);
        }
        else
        {
            SwitchMap(InputMap.Console);
        }
    }

    public void SwitchMap(InputMap map)
    {
        if (currentActionMap != null)
        {
            currentActionMap.Disable();
            previousActionMap = currentActionMap;
        }

        currentActionMap = GetActionMap(map);
        currentActionMap.Enable();
    }

    // NOTE: if you ever need to store a history of maps for whatever reason you could use a stack or pushdown automata
    public void SwitchToPreviousMap()
    {
        if (previousActionMap != null)
        {
            currentActionMap.Disable();
            var temp = currentActionMap;
            currentActionMap = previousActionMap;
            previousActionMap = temp;
            currentActionMap.Enable();

            if (showDebugs) Debug.Log("Switched to previous map " + currentActionMap);
        }
    }

    // Switch Input Action Map
    private InputActionMap GetActionMap(InputMap map)
    {
        currentInputMap = map;
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
