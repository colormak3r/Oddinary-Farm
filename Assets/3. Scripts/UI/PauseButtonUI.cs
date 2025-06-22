using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseButtonUI : UIBehaviour
{
    public static PauseButtonUI Main;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PauseButtonClicked()
    {
        Debug.Log("Pause button clicked");
        //HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
        InputManager.Main.SwitchMap(InputMap.UI);
    }
}
