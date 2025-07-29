/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/28/2025
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/

public class SinglePlayerUI : UIBehaviour
{
    public static SinglePlayerUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public void OnNewGameButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        ConnectionManager.Main.StartGameSinglePlayer();
    }

    public void OnAppearanceButtonClicked()
    {
        AppearanceUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnPetSelectionButtonClicked()
    {
        PetSelectionUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        PlayUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}