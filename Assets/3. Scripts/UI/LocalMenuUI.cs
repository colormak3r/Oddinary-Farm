/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/28/2025
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

public class LocalMenuUI : UIBehaviour
{
    public static LocalMenuUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public void OnMultiplayerLocalHostButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        ConnectionManager.Main.StartGameMultiplayerLocalHost();
    }

    public void OnMultiplayerLocalClientButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        ConnectionManager.Main.StartGameMultiplayerLocalClient();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        PlayUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
