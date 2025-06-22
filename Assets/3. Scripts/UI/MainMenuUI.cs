using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : UIBehaviour
{
    public static MainMenuUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    protected override void Start()
    {
        base.Start();

        StartCoroutine(ControllerElementCoroutine());
    }

    private IEnumerator ControllerElementCoroutine()
    {
        var gamepads = Gamepad.all;
        if (gamepads.Count > 0)
        {
            EventSystem.current.firstSelectedGameObject = firstElement;

            while (EventSystem.current.firstSelectedGameObject == null)
            {
                EventSystem.current.SetSelectedGameObject(firstElement);
                yield return null;
            }
        }

        Debug.Log($"ControllerElementCoroutine exited");
    }

    public void PlayButtonClicked()
    {
        HideNoFade();
        PlayUI.Main.Show();
    }

    public void OptionsButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
    }

    public void CreditsButtonClicked()
    {
        HideNoFade();
        CreditsUI.Main.Show();
    }

    public void QuitButtonClicked()
    {
        Application.Quit();
    }
}
