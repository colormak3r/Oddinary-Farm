using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using static Steamworks.InventoryItem;
using static UnityEngine.Rendering.DebugUI;
public class WalletManager : NetworkBehaviour
{
    public static WalletManager Main;

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

    }

    [Header("Wallet Settings")]
    [SerializeField]
    private ulong initialGlobalWallet = 10;
    [SerializeField]
    private float flushInterval = 0.1f;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private NetworkVariable<ulong> GlobalWallet = new NetworkVariable<ulong>(0);
    [SerializeField]
    private ulong localSpending = 0;
    public ulong LocalWalletValue => GlobalWallet.Value - localSpending;
    public ulong GlobalWalletValue => GlobalWallet.Value;
    [SerializeField]
    private uint pendingAdd = 0;
    private float nextFlushTime = 0f;

    public Action<ulong> OnLocalWalletChanged;
    public Action<ulong> OnGlobalWalletChanged;

    public override void OnNetworkSpawn()
    {
        GlobalWallet.OnValueChanged += HandleGlobalWalletChanged;
        HandleGlobalWalletChanged(0, GlobalWallet.Value);
    }

    public override void OnNetworkDespawn()
    {
        GlobalWallet.OnValueChanged -= HandleGlobalWalletChanged;
    }

    private void HandleGlobalWalletChanged(ulong previousValue, ulong newValue)
    {
        StatisticsManager.Main.UpdateStat(StatisticType.GlobalCoinsCollected, newValue);
        OnLocalWalletChanged?.Invoke(newValue - localSpending);
        OnGlobalWalletChanged?.Invoke(newValue);
    }

    public void InitializeOnServer()
    {
        if (!IsServer) return;

        GlobalWallet.Value += initialGlobalWallet;
    }

    public void AddToWallet(uint amount)
    {
        pendingAdd += amount;
        nextFlushTime = Time.time + flushInterval;
    }

    private void Update()
    {
        if (Time.time >= nextFlushTime) FlushPendingAdd();
    }

    private ulong personalCoinsCollected = 0;
    private void FlushPendingAdd()
    {
        if (pendingAdd == 0) return;

        // Update statistics for personal coins collected
        personalCoinsCollected += pendingAdd;
        StatisticsManager.Main.UpdateStat(StatisticType.PersonalCoinsCollected, personalCoinsCollected);

        // Add the pending amount to the global wallet
        AddToWalletRpc(pendingAdd);
        pendingAdd = 0;
        nextFlushTime = Time.time + flushInterval;
    }

    [Rpc(SendTo.Server)]
    private void AddToWalletRpc(uint amount)
    {
        GlobalWallet.Value += amount;
    }

    public void RemoveFromWallet(uint amount)
    {
        if (amount > GlobalWallet.Value)
        {
            if (showDebugs) Debug.LogWarning("Attempted to remove more from wallet than available. Setting local spending equal too global wallet (LocalWallet now = 0)");
            // Set the local spending to the global wallet value, effectively making the local wallet empty.
            localSpending = GlobalWallet.Value;
        }
        else
        {
            localSpending += amount;
        }

        OnLocalWalletChanged?.Invoke(GlobalWallet.Value - localSpending);
    }
}
