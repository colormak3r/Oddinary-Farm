using ColorMak3r.Utility;
using Steamworks;
using TMPro;
using UnityEngine;

public class GameOverUI : UIBehaviour
{
    public static GameOverUI Main;
    [Header("Settings")]
    [SerializeField]
    private TMP_Text gameoverText;
    [SerializeField]
    [TextArea]
    private string[] escapeText;
    [SerializeField]
    [TextArea]
    private string[] deathText;

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
    }

    public void SetGameoverText(bool escaped)
    {
        if (escaped)
        {
            gameoverText.text = $"And thus, {SteamClient.Name} escaped,\n{escapeText.GetRandomElement()}";
        }
        else
        {
            gameoverText.text = $"And thus, {SteamClient.Name} stayed behind,\n{deathText.GetRandomElement()}";
        }
    }
}