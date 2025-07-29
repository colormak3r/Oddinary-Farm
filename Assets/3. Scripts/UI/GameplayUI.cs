/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/21/2025
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using UnityEngine;

public class GameplayUI : UIBehaviour
{
    private static string FULLSCREEN_VAL = "Fullscreen";
    private static string WINDOWED_VAL = "Windowed";

    public static GameplayUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentScreenMode = PlayerPrefs.GetString("ScreenMode", FULLSCREEN_VAL); // Default to FULLSCREEN_VAL if not set
        showItemRange = PlayerPrefs.GetInt("ShowItemRange", 0) == 1;    // Default to false if not set
        showPlayerName = PlayerPrefs.GetInt("ShowPlayerName", 1) == 1;  // Default to true if not set

        if (currentScreenMode == FULLSCREEN_VAL)
        {
            fullscreenCheckbox.IsChecked = true;
            windowedCheckbox.IsChecked = false;
            Screen.fullScreen = true;
        }
        else
        {
            fullscreenCheckbox.IsChecked = false;
            windowedCheckbox.IsChecked = true;
            Screen.fullScreen = false;
        }

        itemRangeButton.IsChecked = showItemRange;
        playerNameButton.IsChecked = showPlayerName;
    }

    [Header("Gameplay UI Settings")]
    [SerializeField]
    private CheckboxButton fullscreenCheckbox;
    [SerializeField]
    private CheckboxButton windowedCheckbox;
    [SerializeField]
    private CheckboxButton itemRangeButton;
    [SerializeField]
    private CheckboxButton playerNameButton;
    private bool showItemRange;
    public bool ShowItemRange => showItemRange;
    public Action<bool> OnShowItemRangeChanged;

    private bool showPlayerName;
    public bool ShowPlayerName => showPlayerName;
    public Action<bool> OnShowPlayerNameChanged;

    private string currentScreenMode;

    public void OnFullscreenCheckboxClicked()
    {
        fullscreenCheckbox.IsChecked = true;
        windowedCheckbox.IsChecked = false;
        Screen.fullScreen = true;
        PlayerPrefs.SetString("ScreenMode", FULLSCREEN_VAL); // Save the preference
    }

    public void OnWindowedCheckboxClicked()
    {
        fullscreenCheckbox.IsChecked = false;
        windowedCheckbox.IsChecked = true;
        Screen.fullScreen = false;
        PlayerPrefs.SetString("ScreenMode", WINDOWED_VAL); // Save the preference
    }

    public void OnShowItemRangeClicked()
    {
        showItemRange = !showItemRange;
        PlayerPrefs.SetInt("ShowItemRange", showItemRange ? 1 : 0); // Save the preference
        OnShowItemRangeChanged?.Invoke(showItemRange);
        itemRangeButton.IsChecked = showItemRange;
    }

    public void OnShowPlayerNameClicked()
    {
        showPlayerName = !showPlayerName;
        PlayerPrefs.SetInt("ShowPlayerName", showPlayerName ? 1 : 0); // Save the preference
        OnShowPlayerNameChanged?.Invoke(showPlayerName);
        playerNameButton.IsChecked = showPlayerName;
    }

    public void OnResetTutorialClicked()
    {
        AudioManager.Main.PlayClickSound();
        TutorialUI.Main.ResetTutorial();
    }

    public void OnResetPetCollectionClicked()
    {
        AudioManager.Main.PlayClickSound();
        PetManager.Main.ResetCollectionData();
    }

    public void OnBackButtonClicked()
    {
        HideNoFade();
        OptionsUI.Main.Show();
        AudioManager.Main.PlayClickSound();
    }
}
