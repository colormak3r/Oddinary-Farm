using System;
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
    private NetworkVariable<ulong> currentGlobalWallet = new NetworkVariable<ulong>(10);
    [SerializeField]
    private ulong localSpending = 0;
    public ulong LocalWallet => currentGlobalWallet.Value - localSpending;
    [SerializeField]
    private uint pendingAdd = 0;
    private float nextFlushTime = 0f;

    public override void OnNetworkSpawn()
    {
        currentGlobalWallet.OnValueChanged += OnGlobalWalletChanged;
        OnGlobalWalletChanged(0, currentGlobalWallet.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentGlobalWallet.OnValueChanged -= OnGlobalWalletChanged;
    }

    private void OnGlobalWalletChanged(ulong previousValue, ulong newValue)
    {
        InventoryUI.Main.UpdateWallet(newValue - localSpending);
        StatisticsManager.Main.UpdateStat(StatisticType.GlobalCoinsCollected, newValue);
    }

    public void InitializeOnServer()
    {
        if (!IsServer) return;

        currentGlobalWallet.Value += initialGlobalWallet;
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
        currentGlobalWallet.Value += amount;
    }

    public void RemoveFromWallet(uint amount)
    {
        if (amount > currentGlobalWallet.Value)
        {
            Debug.LogWarning("Attempted to remove more from wallet than available. Setting local spending equal too global wallet (LocalWallet now = 0)");
            localSpending = currentGlobalWallet.Value;
        }
        else
        {
            localSpending += amount;
        }

        InventoryUI.Main.UpdateWallet(LocalWallet);
    }
}
