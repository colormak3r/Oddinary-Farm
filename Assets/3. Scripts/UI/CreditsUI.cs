using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CreditsUI : UIBehaviour
{
    //note** - used Options UI code as a base code

    public static CreditsUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(transform.parent.gameObject);

        skipButtonText = skipButton.GetComponentInChildren<TMP_Text>();
    }

    [Header("Credits UI Settings")]
    [SerializeField]
    private GameObject creditsPanel; //game obj that has the scrollable credits
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private Image background;

    private TMP_Text skipButtonText;

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
        if (scene.buildIndex == 0)
        {
            skipButtonText.text = "Skip";
            skipButton.onClick.RemoveListener(SkipButtonClicked);
            skipButton.onClick.AddListener(SkipButtonClicked);
            background.enabled = false;
        }
        else
        {
            skipButtonText.text = "Skip";
            skipButton.onClick.RemoveListener(SkipButtonClicked);
            skipButton.onClick.AddListener(SkipButtonClicked);
            background.enabled = true;
        }
    }

    public void SkipButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
    }

    private void HideCredits()
    {
        creditsPanel.SetActive(false);
    }
}
