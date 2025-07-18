using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

[Serializable]
public class RadioEvent
{
    public int day;
    public int hour;
    [TextArea]
    public string message;
    public AudioClip audioClip;
}

public class RadioManager : NetworkBehaviour
{
    [SerializeField]
    private List<RadioEvent> radioEvents = new List<RadioEvent>();

    public static RadioManager Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    private bool hasShownActivationMessage = false;
    public NetworkVariable<bool> IsActivated = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        IsActivated.OnValueChanged += HandleIsActivatedChanged;

        if (IsActivated.Value)
        {
            SubscribeToTimeManager();
        }
    }
    public override void OnNetworkDespawn()
    {
        IsActivated.OnValueChanged -= HandleIsActivatedChanged;
        UnsubscribeFromTimeManager();
    }

    private void HandleIsActivatedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            SubscribeToTimeManager();

            if (!hasShownActivationMessage && IsClient)
            {
                hasShownActivationMessage = true;
                RadioUI.Main?.DisplayMessage("Radio activated. Signal is live.");
            }

            RadioUI.Main?.SetRadioColor(Color.white);
        }
    }

    private void SubscribeToTimeManager()
    {
        if (TimeManager.Main != null)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        }
    }

    private void UnsubscribeFromTimeManager()
    {
        if (TimeManager.Main != null)
        {
            TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
        }
    }

    private void HandleOnHourChanged(int currentHour)
    {
        int currentDay = TimeManager.Main.CurrentDate;

        foreach (var radioEvent in radioEvents)
        {
            if (radioEvent.day == currentDay && radioEvent.hour == currentHour)
            {

                if (RadioUI.Main != null)
                {
                    RadioUI.Main.DisplayMessage(radioEvent.message);
                }

                break;
                //one message per hour
            }
        }
    }

    public void SetActivated()
    {
        if (IsServer)
        {
            IsActivated.Value = true;
        }
        else
        {
            SetActivatedRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void SetActivatedRpc()
    {
        IsActivated.Value = true;
    }
}
