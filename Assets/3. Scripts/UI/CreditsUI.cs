using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CreditsUI : UIBehaviour
{
    public static CreditsUI Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(transform.parent.gameObject);

        skipButtonText = skipButton.GetComponentInChildren<TMP_Text>();

        CreditsContent();
    }

    [Header("Credits UI Settings")]
    [SerializeField]
    private GameObject creditsPanel;
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private TMP_Text skipButtonText;
    [SerializeField]
    private TMP_Text creditsText;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text roleText;

    private void CreditsContent()
    {
        string[] names = {
            "Khoa Nguyen", "Ryan Carpenter", "Angelica Atega Gangoso", "Susana Garcia", "Roby Ho", "Eunice Kim",
            "Ariana Majerus", "Nicholas Ng", "Logan Thanh-Robertson", "Emily Tsai"
        };

        string[] roles = {
            "Game Director, Lead Programmer", "Associate Director, Programmer", "Writer", "Pixel Artist", "Marketing & Community Manager", "Pixel Artist",
            "Music Composer, Concept Artist, Pixel Artist", "Concept Artist", "QA & Play Tester", "Programmer"
        };

        string creditsOutput = "";

        for (int i = 0; i < names.Length; i++)
        {
            creditsOutput += $"<align=center><size=18>{names[i]}</size>\n<size=9><color=#703A3F>{roles[i]}</color></size></align>\n\n";
        }

        creditsText.text = creditsOutput;
        creditsText.margin = new Vector4(50, 10, 50, 0);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            skipButtonText.text = "Skip";
            skipButton.onClick.RemoveListener(SkipButtonClicked);
            skipButton.onClick.AddListener(SkipButtonClicked);
        }
        else
        {
            skipButtonText.text = "Skip";
            skipButton.onClick.RemoveListener(SkipButtonClicked);
            skipButton.onClick.AddListener(SkipButtonClicked);
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
