/*
 * TunnelingController.cs
 * 
 * This script manages the tunneling state of an entity in a networked game.
 * When tunneling is active, it disables the visual and physical representations of the entity.
 * Used as part of an Animal object
 * 
 * Author: Khoa Nguyen
 * Last Modified: 2023-10-01
 */

using System;
using UnityEngine;
using Unity.Netcode;

public class TunnelingController : NetworkBehaviour
{
    [Header("Tunneling Controller Settings")]
    [SerializeField]
    private GameObject graphicObject;
    [SerializeField]
    private GameObject physicObject;
    [SerializeField]
    private Collider2D hitBoxCollier;
    [SerializeField]
    private ParticleSystem tunnelingEffect;

    [Header("Debugs")]
    [SerializeField]
    private NetworkVariable<bool> IsTunneling = new NetworkVariable<bool>(false);
    public bool IsTunnelingValue => IsTunneling.Value;

    public override void OnNetworkSpawn()
    {
        IsTunneling.OnValueChanged += OnTunnelingChanged;
        OnTunnelingChanged(false, IsTunneling.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsTunneling.OnValueChanged -= OnTunnelingChanged;
    }

    private void OnTunnelingChanged(bool previousValue, bool newValue)
    {
        // Turn these off when tunneling
        graphicObject.SetActive(!newValue);
        physicObject.SetActive(!newValue);
        hitBoxCollier.enabled = !newValue;
        if (newValue)
            tunnelingEffect.Play();
        else
            tunnelingEffect.Stop();
    }

    public void SetTunneling(bool value)
    {
        //Should be set by the server only
        if (!IsServer) return;
        IsTunneling.Value = value;
    }

    [ContextMenu("Toggle Tunneling")]
    private void ToggleTunneling()
    {
        // Should be set by the server only
        if (!IsServer) return;
        IsTunneling.Value = !IsTunneling.Value;
    }
}