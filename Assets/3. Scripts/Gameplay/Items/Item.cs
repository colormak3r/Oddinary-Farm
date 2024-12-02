using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;

public class Item : NetworkBehaviour
{
    [Header("Debugs")]
    [SerializeField]
    protected bool showDebug;
    [SerializeField]
    protected bool showGizmos;
    [SerializeField]
    protected NetworkVariable<ItemProperty> Property = new NetworkVariable<ItemProperty>();
    private ItemProperty property;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponentInParent<AudioSource>();
    }

    public ItemProperty PropertyValue
    {
        get { return Property.Value; }
        set { Property.Value = value; }
    }

    public override void OnNetworkSpawn()
    {
        HandleOnPropertyChanged(null, PropertyValue);
        Property.OnValueChanged += HandleOnPropertyChanged;
    }

    public override void OnNetworkDespawn()
    {
        Property.OnValueChanged -= HandleOnPropertyChanged;
    }

    protected virtual void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        property = newValue;
    }

    public virtual bool CanPrimaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnPrimaryAction(Vector2 position)
    {
        if (property.PrimarySound) PlayPrimarySoundRpc();
    }

    public virtual bool CanSecondaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnSecondaryAction(Vector2 position)
    {
        if (property.SecondarySound) PlaySecondarySoundRpc();
    }

    public virtual bool CanAlternativeAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnAlternativeAction(Vector2 position)
    {
        if (property.AlternativeSound) PlayAlternativeSoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayPrimarySoundRpc()
    {
        audioSource.PlayOneShot(property.PrimarySound);
    }

    [Rpc(SendTo.Everyone)]
    private void PlaySecondarySoundRpc()
    {
        audioSource.PlayOneShot(property.SecondarySound);
    }

    [Rpc(SendTo.Everyone)]
    private void PlayAlternativeSoundRpc()
    {
        audioSource.PlayOneShot(property.AlternativeSound);
    }

    protected bool IsInRange(Vector2 position)
    {
        return ((Vector2)transform.position - position).magnitude < property.Range;
    }

    public void SetGizmosVisibility(bool value)
    {
        showGizmos = value;
    }

    public void SetDebugVisibility(bool value)
    {
        showDebug = value;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, property.Range);
    }
}
