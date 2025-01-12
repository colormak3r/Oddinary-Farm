using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using UnityEngine.Rendering.Universal;

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

    public ItemProperty PropertyValue
    {
        get { return Property.Value; }
        set { Property.Value = value; }
    }

    private void Awake()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => transform.root != null);
        Initialize();
    }

    protected virtual void Initialize()
    {
        audioSource = transform.root.GetComponent<AudioSource>();
        if (!audioSource) Debug.LogError("AudioSource not found in parent object", this);
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

    public virtual void OnPreview(Vector2 position, Previewer previewer)
    {
        previewer.Show(false);
    }

    protected bool IsInRange(Vector2 position)
    {
        return ((Vector2)transform.position - position).magnitude < property.Range;
    }

    #region Primary Action

    public virtual bool CanPrimaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnPrimaryAction(Vector2 position)
    {
        if (property.PrimarySound) PlayPrimarySoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayPrimarySoundRpc()
    {
        audioSource.PlayOneShot(property.PrimarySound);
    }

    #endregion

    #region Secondary Action
    public virtual bool CanSecondaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnSecondaryAction(Vector2 position)
    {
        if (property.SecondarySound) PlaySecondarySoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlaySecondarySoundRpc()
    {
        audioSource.PlayOneShot(property.SecondarySound);
    }

    #endregion

    #region Alternative Action
    public virtual bool CanAlternativeAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnAlternativeAction(Vector2 position)
    {
        if (property.AlternativeSound) PlayAlternativeSoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayAlternativeSoundRpc()
    {
        audioSource.PlayOneShot(property.AlternativeSound);
    }
    #endregion

    #region Utility
    public void SetGizmosVisibility(bool value)
    {
        showGizmos = value;
    }

    public void SetDebugVisibility(bool value)
    {
        showDebug = value;
    }
    #endregion
}
