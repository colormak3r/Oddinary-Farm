/*
 * Created By:      Emily Tsai
 * Date Created:    --/--/----
 * Last Modified:   07/28/2025 (Khoa)
 * Notes:           <write here>
*/


using UnityEngine;
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

        CreditsContent();
    }

    [Header("Credits UI Settings")]
    [SerializeField]
    private TMP_Text skipButtonText;
    [SerializeField]
    private TMP_Text creditsText;
    [SerializeField]
    private int linePadding = 10;
    [SerializeField]
    private Color secondaryColor = new Color(0.439f, 0.227f, 0.247f); // RGB: 70, 58, 63

    private void CreditsContent()
    {
        creditsText.text = ""; // Clear existing text
        var color = $"#{ColorUtility.ToHtmlStringRGBA(secondaryColor)}";

        string[] names = {
            "You", "Khoa Nguyen", "Ryan Carpenter", "Emily Tsai", "Angelica Atega Gangoso", "Susana Garcia", "Roby Ho", "Eunice Kim",
            "Ariana Majerus", "Nicholas Ng", "Logan Thanh-Robertson","Special thanks", "Special thanks"
        };

        string[] roles = {
            "Player", "Game Director, Programmer", "Associate Director, Programmer", "Associate Director, Programmer", "Writer", "Pixel Artist", "Marketing & Community Manager", "Pixel Artist",
            "Music Composer, Concept Artist, Pixel Artist", "Concept Artist", "QA & Play Tester", "Members of Cal State Fullerton\nVideo Game Development Club", "Many friends and family members of our team"
        };

        string creditsOutput = "";
        for (int i = 0; i < linePadding; i++)
        {
            creditsOutput += "\n"; // Add padding lines
        }

        creditsOutput += "\n<size=20>From</size>\n";
        creditsOutput += $"<color={color}><size=36>Spring Roll Studios</size></color>";
        creditsOutput += $"\n<size=20>with love\n\n<color={color}>~~~~~</color>\n\n</size>";

        for (int i = 0; i < names.Length; i++)
        {
            creditsOutput += $"<size=20>{names[i]}</size>\n<size=10><color={color}>{roles[i]}</color>\n\n\n</size>";
        }

        for (int i = 0; i < linePadding; i++)
        {
            creditsOutput += "\n"; // Add padding lines
        }

        creditsOutput += $"<size=20>\n\n<color={color}>Thanks For Playing!</color>\n</size>";

        for (int i = 0; i < linePadding; i++)
        {
            creditsOutput += "\n"; // Add padding lines
        }

        creditsOutput += "<size=0>~~~~~";
        creditsText.text = creditsOutput;
    }

    public void SkipButtonClicked()
    {
        HideNoFade();
        MainMenuUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
