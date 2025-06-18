using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
