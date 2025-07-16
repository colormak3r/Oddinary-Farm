using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : UIBehaviour
{
    public static UpgradeUI Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    [Header("UI Elements")]
    [SerializeField]
    private TMP_Text promptText;
    [SerializeField]
    private TMP_Text costText;
    [SerializeField]
    private Image currentStageImage;
    [SerializeField]
    private Image nextStageImage;
    [SerializeField]
    private Button upgradeButton;

    [Header("Debugs")]
    [SerializeField]
    private PlayerInventory inventory;
    [SerializeField]
    private UpgradeStages upgradeStages;
    [SerializeField]
    private int currentStage;
    [SerializeField]
    private Action upgradeCallback;

    public void Initialize(PlayerInventory inventory, UpgradeStages upgradeStages, int currentStage, Action upgradeCallback)
    {
        this.inventory = inventory;
        this.upgradeStages = upgradeStages;
        this.upgradeCallback = upgradeCallback;
        this.currentStage = currentStage;

        UpdateStage(this.upgradeStages, currentStage);
        Show();
    }

    private void UpdateStage(UpgradeStages upgradeStages, int currentStage)
    {
        currentStageImage.sprite = upgradeStages.GetStage(currentStage).sprite;
        nextStageImage.sprite = upgradeStages.GetStage(currentStage + 1).sprite;
        promptText.text = upgradeStages.GetStage(currentStage + 1).prompt + $" ({currentStage + 1}/{upgradeStages.GetStageCount() - 1})";

        var multiplier = (ulong)NetworkManager.Singleton.ConnectedClients.Count;
        var cost = upgradeStages.GetStage(currentStage + 1).cost * multiplier;
        costText.text = cost.ToString();
        upgradeButton.interactable = WalletManager.Main.LocalWalletValue > cost;
    }

    public void OnUpgradeButtonClicked()
    {
        var price = (uint)upgradeStages.GetStage(currentStage + 1).cost * (uint)NetworkManager.Singleton.ConnectedClients.Count;
        inventory.ConsumeCoinsOnClient(price);
        currentStage++;
        if (currentStage < upgradeStages.GetStageCount() - 1)
            UpdateStage(upgradeStages, currentStage);
        else
            Hide();

        upgradeCallback.Invoke();
    }
}