/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/03/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using Unity.Netcode;
using UnityEngine;

public enum ChestState
{
    Unopened,
    Opened,
    Empty
}

public class Chest : NetworkBehaviour, IInteractable
{
    [Header("Chest Settings")]
    [SerializeField]
    private Sprite unopenedSprite;
    [SerializeField]
    private Sprite openedSprite;
    [SerializeField]
    private Sprite emptySprite;
    [SerializeField]
    private AudioClip openSound;
    [SerializeField]
    private AudioClip collectSound;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private NetworkVariable<ChestState> CurrentState = new NetworkVariable<ChestState>(global::ChestState.Unopened);
    public ChestState CurrentStateValue => CurrentState.Value;
    public bool IsHoldInteractable => false;

    private AudioElement audioElement;

    private void Awake()
    {
        audioElement = GetComponent<AudioElement>();
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnChestStateChanged;
        OnChestStateChanged(ChestState.Unopened, CurrentStateValue);
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnChestStateChanged;
    }

    private void OnChestStateChanged(ChestState previousValue, ChestState newValue)
    {
        switch (newValue)
        {
            case ChestState.Unopened:
                spriteRenderer.sprite = unopenedSprite;
                break;
            case ChestState.Opened:
                spriteRenderer.sprite = openedSprite;
                audioElement.PlayOneShot(openSound);
                break;
            case ChestState.Empty:
                spriteRenderer.sprite = emptySprite;
                audioElement.PlayOneShot(collectSound);
                GetComponent<SelectorModifier>().SetCanBeSelected(false);
                if (IsServer && TryGetComponent<ObservabilityController>(out var controller))
                    controller.EndObservabilityOnServer();
                break;
        }
    }

    public void Interact(Transform source)
    {
        if (CurrentStateValue == ChestState.Unopened)
        {
            UpdateStateRpc(ChestState.Opened);
        }
        else if (CurrentStateValue == ChestState.Opened)
        {
            Selector.Main?.Show(false);
            UpdateStateRpc(ChestState.Empty, source.gameObject);
        }
        else if (CurrentStateValue == ChestState.Empty)
        {
            // Do nothing
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateStateRpc(ChestState newState, NetworkObjectReference sourceRef = default)
    {
        CurrentState.Value = newState;

        if (newState == ChestState.Empty && sourceRef.TryGet(out NetworkObject sourceObject))
        {
            GetComponent<LootGenerator>()?.DropLootOnServer(sourceObject);
        }
    }

    public void InteractionStart(Transform source)
    {
        throw new NotImplementedException();
    }

    public void InteractionEnd(Transform source)
    {
        throw new NotImplementedException();
    }
}
