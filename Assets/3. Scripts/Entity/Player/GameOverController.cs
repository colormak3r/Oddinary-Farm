using System;
using Unity.Netcode;
using UnityEngine;

public class GameOverController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private int gameOverDate = 8;
    [SerializeField]
    private int gameOverHour = 0;

    public override void OnNetworkSpawn()
    {
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
    }

    public override void OnNetworkDespawn()
    {
        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
    }

    private void OnHourChanged(int currentHour)
    {
        if (TimeManager.Main.CurrentDate > gameOverDate || (TimeManager.Main.CurrentDate == gameOverDate && TimeManager.Main.CurrentHour >= gameOverHour))
        {
            GameManager.Main.GameOver(GetComponent<HotAirBalloonController>().IsControlledValue);
            GetComponent<ControllableController>().SetControl(false);
        }
    }
}