using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Plant : NetworkBehaviour, IWaterable
{
    [Header("Settings")]
    [SerializeField]
    private PlantProperty mockProperty;

    private NetworkVariable<int> CurrentStage = new NetworkVariable<int>();

    private NetworkVariable<FixedString128Bytes> Property = new NetworkVariable<FixedString128Bytes>();
    private PlantProperty currentProperty;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        HandlePropertyChanged(Property.Value, Property.Value);
        Property.OnValueChanged += HandlePropertyChanged;
        CurrentStage.OnValueChanged += HandleStageChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandlePropertyChanged;
    }

    private void HandlePropertyChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        HandlePropertyChanged(current.ToString());
    }

    private void HandlePropertyChanged(string item)
    {
        currentProperty = (PlantProperty)AssetManager.Main.GetAssetByName(item);
        if (currentProperty == null) return;

        HandleStageChanged(CurrentStage.Value);
    }

    private void HandleStageChanged(int previous, int current)
    {
        HandleStageChanged(current);
    }

    private void HandleStageChanged(int stage)
    {
        if (currentProperty == null) return;

        spriteRenderer.sprite = currentProperty.Stages[stage].sprite;
    }

    [ContextMenu("Mock Property Change")]
    public void MockPropertyChange()
    {
        if (!IsHost) return;
        Property.Value = mockProperty.name;
    }

    public void Initialize(PlantProperty property)
    {
        Property.Value = property.name;
        CurrentStage.Value = 0;
    }

    public void GetWatered()
    {
        GetWateredRpc();
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc()
    {
        if (CurrentStage.Value < currentProperty.Stages.Length - 1)
            CurrentStage.Value++;
    }
}
