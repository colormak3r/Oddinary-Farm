/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/28/2025
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

public class SteamMenuUI : UIBehaviour
{
    public static SteamMenuUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public void OnMultiplayerSteamHostButtonClicked()
    {
        HideNoFade();
        LobbyUI.Main.Host();
        AudioManager.Main.PlayClickSound();
    }

    public void OnMultiplayerSteamClientButtonClicked()
    {
        HideNoFade();
        LobbyUI.Main.Client();
        AudioManager.Main.PlayClickSound();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        PlayUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}