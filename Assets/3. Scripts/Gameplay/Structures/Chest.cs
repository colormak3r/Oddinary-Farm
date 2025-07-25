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
    Buried,
    Unopened,
    Opened,
    Empty
}

public class Chest : NetworkBehaviour, IInteractable, IDiggable
{
    [Header("Chest Settings")]
    [SerializeField]
    private ChestState defaultState = ChestState.Unopened;
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Chest Sprites")]
    [SerializeField]
    private Sprite buriedSprite;
    [SerializeField]
    private Sprite unopenedSprite;
    [SerializeField]
    private Sprite openedSprite;
    [SerializeField]
    private Sprite emptySprite;

    [Header("Chest Audio")]
    [SerializeField]
    private AudioClip digSound;
    [SerializeField]
    private AudioClip openSound;
    [SerializeField]
    private AudioClip collectSound;

    [Header("Chest Debugs")]
    [SerializeField]
    private NetworkVariable<ChestState> CurrentState = new NetworkVariable<ChestState>();
    public ChestState CurrentStateValue => CurrentState.Value;
    public bool IsHoldInteractable => false;

    private AudioElement audioElement;
    private SelectorModifier selectorModifier;

    private void Awake()
    {
        audioElement = GetComponent<AudioElement>();
        selectorModifier = GetComponent<SelectorModifier>();
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnChestStateChanged;
        if (IsServer) CurrentState.Value = defaultState;
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
            case ChestState.Buried:
                spriteRenderer.sprite = buriedSprite;
                selectorModifier.SetCanBeSelected(false);
                break;
            case ChestState.Unopened:
                spriteRenderer.sprite = unopenedSprite;
                selectorModifier.SetCanBeSelected(true);
                if (previousValue == ChestState.Buried)
                    audioElement.PlayOneShot(digSound);
                break;
            case ChestState.Opened:
                spriteRenderer.sprite = openedSprite;
                selectorModifier.SetCanBeSelected(true);
                audioElement.PlayOneShot(openSound);
                break;
            case ChestState.Empty:
                spriteRenderer.sprite = emptySprite;
                audioElement.PlayOneShot(collectSound);
                selectorModifier.SetCanBeSelected(false);
                if (IsServer && TryGetComponent<ObservabilityController>(out var controller))
                    controller.EndObservabilityOnServer();
                break;
        }
    }

    public void Interact(Transform source)
    {
        if (CurrentStateValue == ChestState.Buried)
        {
            // Do nothing, cannot interact with a buried chest
            // Need to use shovel to dig it up first
        }
        else if (CurrentStateValue == ChestState.Unopened)
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

    public void Dig(Transform source)
    {
        if (CurrentStateValue == ChestState.Buried)
        {
            UpdateStateRpc(ChestState.Unopened);
            selectorModifier.SetCanBeSelected(true);
            if (source.TryGetComponent(out PlayerInteraction playerInteraction))
                playerInteraction.ClearCurrentInteractable();
        }
    }
}
