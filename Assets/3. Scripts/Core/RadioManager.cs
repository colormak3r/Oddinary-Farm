using UnityEngine;
using Unity.Netcode;

public class RadioManager : NetworkBehaviour
{
    public static RadioManager Main;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    private NetworkVariable<bool> IsActivated = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        IsActivated.OnValueChanged += HandleIsActivatedChanged;
    }
    public override void OnNetworkDespawn()
    {
        IsActivated.OnValueChanged -= HandleIsActivatedChanged;
    }

    private void HandleIsActivatedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        }
    }

    private void HandleOnHourChanged(int currentHour)
    {
        Debug.Log($"Current hour is {currentHour}");
    }

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
