/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameUI : UIBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showPlayerName = true;
    [SerializeField]
    private int maxLength = 12;
    [SerializeField]
    private TMP_Text playerNameText;
    [SerializeField]
    private float showNameRange = 5f;
    [SerializeField]
    private float scanInterval = 1f;
    [SerializeField]
    private LayerMask playerLayer;

    private float nextScan;
    private Coroutine autoHideCoroutine;
    private HashSet<string> displayedPlayerNames = new HashSet<string>();

    protected override void Start()
    {
        base.Start();
        if(GameplayUI.Main == null)
        {
            Debug.LogError("GameplayUI.Main is not initialized. Ensure GameplayUI is loaded before PlayerNameUI.");
            return;
        }
        showPlayerName = GameplayUI.Main.ShowPlayerName; // Get the initial value from GameplayUI
        GameplayUI.Main.OnShowPlayerNameChanged += SetShowPlayerName;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (GameplayUI.Main != null)
        {
            GameplayUI.Main.OnShowPlayerNameChanged -= SetShowPlayerName;
        }
    }

    public void SetPlayerName(string playerName)
    {
        playerNameText.text = playerName.Length > maxLength ? playerName.Substring(0, maxLength) + (playerName.Length > maxLength ? "..." : "") : playerName;
        ShowPlayerName();
    }

    public void SetShowPlayerName(bool showPlayerName)
    {
        this.showPlayerName = showPlayerName;
    }

    public void ShowPlayerName()
    {
        if (!showPlayerName) return;

        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
    }

    private IEnumerator AutoHideCoroutine()
    {
        Show();
        yield return new WaitForSeconds(3f);
        Hide();
    }

    private void Update()
    {
        if (!showPlayerName) return;

        if (Time.time < nextScan) return;
        nextScan = Time.time + scanInterval;

        HashSet<string> currentNearbyPlayers = new HashSet<string>();

        var nearbyPlayers = Physics2D.OverlapCircleAll(transform.position, showNameRange, playerLayer);
        foreach (var player in nearbyPlayers)
        {
            if (player.TryGetComponent(out PlayerStatus status))
            {
                if (!displayedPlayerNames.Contains(status.PlayerNameValue))
                {
                    displayedPlayerNames.Add(status.PlayerNameValue);
                    status.PlayerNameUI.ShowPlayerName();
                }

                currentNearbyPlayers.Add(status.PlayerNameValue);
            }
        }

        // Remove players that are no longer nearby
        List<string> namesToRemove = new List<string>();
        foreach (string playerName in displayedPlayerNames)
        {
            if (!currentNearbyPlayers.Contains(playerName))
            {
                namesToRemove.Add(playerName);
            }
        }

        foreach (string playerName in namesToRemove)
        {
            displayedPlayerNames.Remove(playerName);
        }
    }
}