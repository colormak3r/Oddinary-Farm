using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsUI : UIBehaviour
{
    public static OptionsUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(transform.parent.gameObject);

        backButtonText = backButton.GetComponentInChildren<TMP_Text>();
    }

    [Header("Options UI Settings")]
    [SerializeField]
    private GameObject leaveButton;
    [SerializeField]
    private GameObject appearanceButton;
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button statButton;

    private TMP_Text backButtonText;

    public override void OnSceneChanged(Scene scene)
    {
        base.OnSceneChanged(scene);

        if (scene.buildIndex == 0)
        {
            leaveButton.SetActive(false);
            appearanceButton.SetActive(false);
            resumeButton.gameObject.SetActive(false);
            backButton.gameObject.SetActive(true);
            statButton.gameObject.SetActive(false);
        }
        else
        {
            leaveButton.SetActive(true);
            appearanceButton.SetActive(true);
            resumeButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(false);
            statButton.gameObject.SetActive(true);
        }
    }

    public void AudioButtonClicked()
    {
        AudioUI.Main.Initialize();
        HideNoFade();
        AudioUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void GameplayButtonClicked()
    {
        HideNoFade();
        GameplayUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void LeaveButtonClicked()
    {
        Hide();
        GameManager.Main.ReturnToMainMenu();
        AudioManager.Main.PlayClickSound();
    }

    public void BackButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }

    public void ResumeButtonClicked()
    {
        AudioManager.Main.PlayClickSound();
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            Debug.LogWarning("This should not happen. Cannot resume in the main menu scene.");
        }
        else
        {
            Hide();
            PauseButtonUI.Main.Show();
            InputManager.Main.SwitchMap(InputMap.Gameplay);
        }
    }

    public void StatButtonClicked()
    {
        HideNoFade();
        StatUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
