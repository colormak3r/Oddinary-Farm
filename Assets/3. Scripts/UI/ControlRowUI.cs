using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControlRowUI : MonoBehaviour
{
    [Header("Control Row Settings")]
    [SerializeField]
    private TMP_Text actionText;
    [SerializeField]
    private TMP_Text keyboardText;
    [SerializeField]
    private TMP_Text controllerText;

    private ControlSetting controlSetting;
    private int kbIndex, gpIndex;

    public void Initialize(ControlSetting controlSetting)
    {
        this.controlSetting = controlSetting;

        actionText.text = controlSetting.ActionName;
        keyboardText.text = controlSetting.CurrentKey_Keyboard != string.Empty ? controlSetting.CurrentKey_Keyboard : controlSetting.DefaultKey_Keyboard;
        controllerText.text = controlSetting.CurrentKey_Controller != string.Empty ? controlSetting.CurrentKey_Controller : controlSetting.DefaultKey_Controller;

        /*var a = InputManager.Main.InputActions.asset.FindAction(controlSetting.ActionName);
        if (a == null)
        {
            Debug.LogError($"Action '{controlSetting.ActionName}' not found in InputActions.");
            return;
        }

        kbIndex = GetBindingIndex(a, "Keyboard");
        gpIndex = GetBindingIndex(a, "Gamepad");

        Refresh();*/
    }

    public void OnKeyboardButtonClicked()
    {
        keyboardText.text = "…press a key";
        /*InputManager.Main.Rebind(controlSetting.ActionName, kbIndex, result =>
        {
            keyboardText.text = result;
        });*/
    }

    public void OnControllerButtonClicked()
    {
        controllerText.text = "…press a button";
        /*InputManager.Main.Rebind(controlSetting.ActionName, gpIndex, result =>
        {
            controllerText.text = result;
        });*/
    }

    public void Refresh()
    {
        var a = InputManager.Main.InputActions.asset.FindAction(controlSetting.ActionName);
        keyboardText.text = HumanName(a, kbIndex);
        controllerText.text = HumanName(a, gpIndex);
    }

    static int GetBindingIndex(InputAction action, string group)
    {
        for (int i = 0; i < action.bindings.Count; ++i)
        {
            var g = action.bindings[i].groups;           // may be null
            if (!string.IsNullOrEmpty(g) && g.Contains(group))
                return i;
        }
        return -1;                                      // not found
    }

    static string HumanName(InputAction action, int index)
    {
        // binding not found => show placeholder
        if (index < 0 || index >= action.bindings.Count)
            return "<none>";

        var path = action.bindings[index].effectivePath;
        return string.IsNullOrEmpty(path)
            ? "<unbound>"
            : InputControlPath.ToHumanReadableString(
                    path,
                    InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}
