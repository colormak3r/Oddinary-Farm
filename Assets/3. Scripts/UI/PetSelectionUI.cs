/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/09/2025
 * Last Modified:   07/09/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections.Generic;
using UnityEngine;

public class PetSelectionUI : UIBehaviour
{
    private static readonly string SELECTED_PET_KEY = "SelectedPet";

    [Header("Components")]
    [SerializeField]
    private Transform gridTransform;
    [SerializeField]
    private GameObject petSelectionButtonPrefab;
    [SerializeField]
    private PetSelectionButton noneButton;

    private List<PetSelectionButton> buttons = new List<PetSelectionButton>();

    protected override void Start()
    {
        base.Start();

        var entries = PetManager.Main.PetDataEntries;
        PetData autoSelectedPet = null;
        var playerPrefString = PlayerPrefs.GetString(SELECTED_PET_KEY, string.Empty);
        foreach (var petData in entries)
        {

            var buttonObj = Instantiate(petSelectionButtonPrefab, gridTransform);
            var button = buttonObj.GetComponent<PetSelectionButton>();
            button.Initialze(petData, OnPetSelected);
            buttons.Add(button);

            // Check if the player has a preference or not
            if (petData.petType.ToString() == playerPrefString)
            {
                autoSelectedPet = petData;
                OnPetSelected(petData, button);
            }
            else
            {
                // If the player has no preference, select the first collected pet
                if (autoSelectedPet == null && PetManager.Main.IsPetCollected(petData.petType))
                {
                    autoSelectedPet = petData;
                    OnPetSelected(petData, button);
                }
            }
        }

        // Move none button to last
        buttons.Add(noneButton);
        noneButton.transform.SetAsLastSibling();

        // Set to none if no pet is selected
        if (autoSelectedPet == null) OnNoneClicked();
    }

    public void OnNoneClicked()
    {
        PetManager.Main.SetPetToSpawn(null);

        // Deselect all buttons
        foreach (var b in buttons)
        {
            b.SetSelected(false);
        }

        noneButton.SetSelected(true);

        AudioManager.Main.PlayClickSound();

        PlayerPrefs.SetString(SELECTED_PET_KEY, string.Empty);
    }

    private void OnPetSelected(PetData petData, PetSelectionButton button)
    {
        if (!PetManager.Main.IsPetCollected(petData.petType)) return;

        PetManager.Main.SetPetToSpawn(petData);

        // Deselect all buttons
        foreach (var b in buttons)
        {
            b.SetSelected(false);
        }
        button.SetSelected(true);

        AudioManager.Main.PlayClickSound();

        PlayerPrefs.SetString(SELECTED_PET_KEY, petData.petType.ToString());
    }

    public void ResetCollectionDataClicked()
    {
        PetManager.Main.ResetCollectionData();
        AudioManager.Main.PlayClickSound();
        OnNoneClicked();
        foreach (var b in buttons)
        {
            if (b == noneButton) continue;
            b.Initialze(b.PetData, OnPetSelected);
        }
    }
}