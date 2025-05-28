using System;
using TMPro;
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

    private void UpdateStage(UpgradeStages currentStages, int currentStage)
    {
        currentStageImage.sprite = currentStages.GetStage(currentStage).sprite;
        nextStageImage.sprite = currentStages.GetStage(currentStage + 1).sprite;
        promptText.text = currentStages.GetStage(currentStage + 1).prompt;

        var cost = currentStages.GetStage(currentStage + 1).cost;
        costText.text = cost.ToString();
        upgradeButton.interactable = WalletManager.Main.LocalWallet > cost;
    }

    public void OnUpgradeButtonClicked()
    {
        inventory.ConsumeCoinsOnClient((uint)upgradeStages.GetStage(currentStage + 1).cost);
        currentStage++;
        if (currentStage < upgradeStages.GetStageCount() - 1)
            UpdateStage(upgradeStages, currentStage);
        else
            Hide();

        upgradeCallback.Invoke();
    }
}