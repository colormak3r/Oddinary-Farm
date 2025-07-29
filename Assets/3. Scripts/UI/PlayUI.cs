/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/28/2025
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

public class PlayUI : UIBehaviour
{
    public static PlayUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public void OnSinglePlayerClicked()
    {
        HideNoFade();
        SinglePlayerUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnMultiplayerOnlineClicked()
    {
        HideNoFade();
        SteamMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnMultiplayerLocalClicked()
    {
        HideNoFade();
        LocalMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
