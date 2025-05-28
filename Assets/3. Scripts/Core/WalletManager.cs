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

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<ulong> currentGlobalWallet = new NetworkVariable<ulong>(10);
    [SerializeField]
    private ulong localSpending = 0;
    public ulong LocalWallet => currentGlobalWallet.Value - localSpending;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentGlobalWallet.Value = initialGlobalWallet;
        }

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
    }


    public void AddToWallet(uint amount)
    {
        AddToWalletRpc(amount);
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
