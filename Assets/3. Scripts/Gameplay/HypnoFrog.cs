/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/19/2025
 * Last Modified:   07/21/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class HypnoFrog : NetworkBehaviour, IInteractable
{
    [Header("Hypno Frog Settings")]
    [SerializeField]
    private NetworkVariable<bool> HasFrog = new NetworkVariable<bool>();

    private Animator animator;
    private Collider2D interactionCollider;
    private Light2D light2D;

    public bool IsHoldInteractable => false;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        interactionCollider = GetComponent<Collider2D>();
        light2D = GetComponentInChildren<Light2D>();
    }

    public override void OnNetworkSpawn()
    {
        HasFrog.OnValueChanged += HandleOnValueChanged;
        HandleOnValueChanged(false, HasFrog.Value);

        TimeManager.Main.OnHourChanged.AddListener(HandleOnHourChanged);
        HandleOnHourChanged(TimeManager.Main.CurrentHour);
    }

    protected override void OnNetworkPostSpawn()
    {
        for (int x = -6; x <= 6; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                WorldGenerator.Main.RemoveFoliageOnClient(transform.position + new Vector3(x, y));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        HasFrog.OnValueChanged -= HandleOnValueChanged;
        TimeManager.Main.OnHourChanged.RemoveListener(HandleOnHourChanged);
    }

    private void HandleOnHourChanged(int currentHour)
    {
        if (IsServer)
        {
            if (currentHour >= HypnoFrogManager.Main.AppearanceHour || currentHour <= HypnoFrogManager.Main.DisappearanceHour)
            {
                HasFrog.Value = HypnoFrogManager.Main.GetHasFrog(transform.position);
            }
            else
            {
                HasFrog.Value = false;
            }
        }

        light2D.enabled = TimeManager.Main.IsNight;
    }

    private void HandleOnValueChanged(bool previousValue, bool newValue)
    {
        animator.SetBool("HasFrog", newValue);
        interactionCollider.enabled = newValue;
    }

    public void Interact(Transform source)
    {
        if (source.TryGetComponent(out PlayerStatus playerStatus) && playerStatus.CurrentCurse == PlayerCurse.None)
            HypnoFrogUI.Main.Show();
    }

    public void InteractionEnd(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionStart(Transform source)
    {
        throw new System.NotImplementedException();
    }
}
