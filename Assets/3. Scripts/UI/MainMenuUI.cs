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

    public void SinglePlayerButtonClicked()
    {
        ConnectionManager.Main.StartGameSinglePlayer();
    }

    public void MultiplayerLocalHostButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerLocalHost();
    }

    public void MultiplayerLocalClientButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerLocalClient();
    }

    public void MultiplayerOnlineHostButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerOnlineHost();
    }

    public void MultiplayerOnlineClientButtonClicked() 
    { 
        ConnectionManager.Main.StartGameMultiplayerOnlineClient();
    }

    public void OptionsButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
    }

    public void QuitButtonClicked()
    {
        Application.Quit();
    }
}
