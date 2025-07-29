/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : UIBehaviour
{
    public static MainMenuUI Main;
    public static string FEEDBACK_URL = "https://forms.gle/DYbNmeGaRbPtMbmq8";
    public static string CONTACT_LIST_URL = "https://forms.gle/nJ4gDUf7jzH5Rpba8";

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
    }

    public void OnPlayButtonClicked()
    {
        HideNoFade();
        PlayUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnOptionsButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnCreditsButtonClicked()
    {
        HideNoFade();
        CreditsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnContactListButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        Application.OpenURL(CONTACT_LIST_URL);
    }

    public void OnFeedbackButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        Application.OpenURL(FEEDBACK_URL);
    }

    public void OnQuitButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        Application.Quit();
    }
}
