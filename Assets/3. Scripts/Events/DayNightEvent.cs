using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class DayNightEvent : NetworkBehaviour
{
    [Header("Events")]
    [SerializeField]
    private UnityEvent OnDayStart;
    [SerializeField]
    private UnityEvent OnNightStart;

    public override void OnNetworkSpawn()
    {
        TimeManager.Main.OnDayStart.AddListener(HandleOnDayStarted);
        TimeManager.Main.OnNightStart.AddListener(HandleOnNightStarted);

    }

    [ContextMenu("Update Status")]
    public void UpdateStatus()
    {
        if (TimeManager.Main.IsDay)
        {
            Debug.Log("Day started, invoking OnDayStart event.");
            HandleOnDayStarted();
        }
        else
        {
            Debug.Log("Night started, invoking OnNightStart event.");
            HandleOnNightStarted();
        }
    }

    public override void OnNetworkDespawn()
    {
        TimeManager.Main.OnDayStart.RemoveListener(HandleOnDayStarted);
        TimeManager.Main.OnNightStart.RemoveListener(HandleOnNightStarted);
    }

    private void HandleOnDayStarted()
    {
        OnDayStart?.Invoke();
    }

    private void HandleOnNightStarted()
    {
        OnNightStart?.Invoke();
    }

}
