using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerNameUI : UIBehaviour
{
    [Header("Settings")]
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

    public void SetPlayerName(string playerName)
    {
        playerNameText.text = playerName;
        ShowPlayerName();
    }

    public void ShowPlayerName()
    {
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
                    SetPlayerName(status.PlayerNameValue);
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