using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
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
    private NetworkVariable<ulong> GlobalWallet = new NetworkVariable<ulong>(10);
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

    private void FlushPendingAdd()
    {
        if (pendingAdd == 0) return;

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
            Debug.LogWarning("Attempted to remove more from wallet than available. Setting local spending equal too global wallet (LocalWallet now = 0)");
            localSpending = GlobalWallet.Value;
        }
        else
        {
            localSpending += amount;
        }

        OnLocalWalletChanged?.Invoke(GlobalWallet.Value - localSpending);
    }
}
