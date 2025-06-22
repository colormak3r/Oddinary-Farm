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
        }
        else
        {
            leaveButton.SetActive(true);
            appearanceButton.SetActive(true);
            resumeButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(false);
        }
    }

    public void AudioButtonClicked()
    {
        AudioUI.Main.Initialize();
        HideNoFade();
        AudioUI.Main.Show();
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
        HideNoFade();
        PauseButtonUI.Main.Show();
        AudioManager.Main.PlayClickSound();
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {

        }
        else
        {
            InputManager.Main.SwitchMap(InputMap.Gameplay);
        }
    }
}
