using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.GPUSort;

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

    private TMP_Text backButtonText;

    public override void OnSceneChanged(Scene scene)
    {
        base.OnSceneChanged(scene);

        if (scene.buildIndex == 0)
        {
            leaveButton.SetActive(false);
            backButtonText.text = "Back";
            backButton.onClick.RemoveListener(ResumeButtonClicked);
            backButton.onClick.AddListener(BackButtonClicked);
        }
        else
        {
            leaveButton.SetActive(true);
            backButtonText.text = "Resume";
            backButton.onClick.RemoveListener(BackButtonClicked);
            backButton.onClick.AddListener(ResumeButtonClicked);
        }
    }

    public void AudioButtonClicked()
    {
        AudioUI.Main.Initialize();
        HideNoFade();
        AudioUI.Main.Show();
    }

    public void LeaveButtonClicked()
    {
        GameManager.Main.ReturnToMainMenu();
    }

    public void BackButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
    }

    private void ResumeButtonClicked()
    {
        Hide();
    }
}
