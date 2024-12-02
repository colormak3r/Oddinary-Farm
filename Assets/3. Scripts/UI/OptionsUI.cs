using TMPro;
using UnityEngine;
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
    private Button backButton;
    [SerializeField]
    private Image background;

    private TMP_Text backButtonText;

    protected override void OnEnable()
    {
        base.OnEnable();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        backButton.onClick.RemoveAllListeners();
        if (scene.buildIndex == 0)
        {
            leaveButton.SetActive(false);
            backButtonText.text = "Back";
            backButton.onClick.AddListener(BackButtonClicked);
            background.enabled = false;
        }
        else
        {
            leaveButton.SetActive(true);
            backButtonText.text = "Resume";
            backButton.onClick.AddListener(ResumeButtonClicked);
            background.enabled = true;
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
