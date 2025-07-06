using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayUI : UIBehaviour
{
 public static PlayUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);

        backButton.onClick.AddListener(BackButtonClicked);
    }

    [Header("Options UI Settings")]
    [SerializeField]
    private Button backButton;
    public void SinglePlayerButtonClicked()
    {
        ConnectionManager.Main.StartGameSinglePlayer();
    }

    public void MultiplayerOnlineHostButtonClicked()
    {
        ConnectionManager.Main.StartGameMultiplayerOnlineHost();
    }

    public void MultiplayerOnlineClientButtonClicked() 
    { 
        ConnectionManager.Main.StartGameMultiplayerOnlineClient();
    }

    public void BackButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
    }
}
