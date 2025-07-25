
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
    // I usually keep static declarations and awake method at the top of the class for easy access.
    public static RadioManager Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("Radio Events")]
    [SerializeField]
    private List<RadioEvent> radioEvents = new List<RadioEvent>();

    [Header("Radio Debugs")]
    [SerializeField]
    private bool hasShownActivationMessage = false;

    // Try not to expose NetworkVariables directly unless necessary.
    // Usually objects should only read from the manager and not set them
    // Use concrete method such as SetActivated() to change the state if neccessary.
    // => Easy debugging and swapping of implementation later on.
    private NetworkVariable<bool> IsActivated = new NetworkVariable<bool>();
    public bool IsActivatedValue => IsActivated.Value;

    public override void OnNetworkSpawn()
    {
        IsActivated.OnValueChanged += HandleIsActivatedChanged;
        TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
    }
    public override void OnNetworkDespawn()
    {
        IsActivated.OnValueChanged -= HandleIsActivatedChanged;
        TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
    }

    private void HandleIsActivatedChanged(bool oldValue, bool newValue)
    {
        if (newValue && !hasShownActivationMessage)
        {
            hasShownActivationMessage = true;
            RadioUI.Main?.DisplayMessage("Radio activated. Signal is live.");
        }
    }

    private void HandleOnHourChanged(int currentHour)
    {
        if (!IsActivatedValue) return;

        int currentDay = TimeManager.Main.CurrentDate;

        foreach (var radioEvent in radioEvents)
        {
            if (radioEvent.day == currentDay && radioEvent.hour == currentHour)
            {
                RadioUI.Main?.DisplayMessage(radioEvent.message);
                break;
                //one message per hour
            }
        }
    }

    // If method is called on the server, it use no bandwith. No need to check IsServer
    public void SetActivated()
    {
        SetActivatedRpc();
    }

    [Rpc(SendTo.Server)]
    private void SetActivatedRpc()
    {
        IsActivated.Value = true;
    }
}
