using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class Item : MonoBehaviour
{
    [Header("Debugs")]
    [SerializeField]
    protected bool showDebug;
    [SerializeField]
    protected bool showGizmos;
    [SerializeField]
    private ItemProperty baseProperty;
    public ItemProperty BaseProperty => baseProperty;

    public ItemSystem ItemSystem { get; private set; }
    public AudioElement AudioElement { get; private set; }
    public LayerManager LayerManager { get; private set; }

    public virtual void Initialize(ItemProperty baseProperty)
    {
        AudioElement = transform.root.GetComponent<AudioElement>();
        if (!AudioElement) Debug.LogError("AudioElement not found in parent object", this);

        ItemSystem = transform.root.GetComponent<ItemSystem>();
        if (!ItemSystem) Debug.LogError("ItemSystem not found in parent object", this);

        LayerManager = LayerManager.Main;
        this.baseProperty = baseProperty;
    }

    public virtual void OnPreview(Vector2 position, Previewer previewer)
    {
        previewer.Show(false);
    }

    #region Primary Action

    public virtual bool CanPrimaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnPrimaryAction(Vector2 position)
    {
        if (baseProperty.PrimarySound) PlayPrimarySoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayPrimarySoundRpc()
    {
        if (AudioElement == null) return;

        AudioElement.PlayOneShot(baseProperty.PrimarySound);
    }

    #endregion

    #region Secondary Action
    public virtual bool CanSecondaryAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnSecondaryAction(Vector2 position)
    {
        if (baseProperty.SecondarySound) PlaySecondarySoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlaySecondarySoundRpc()
    {
        AudioElement.PlayOneShot(baseProperty.SecondarySound);
    }

    #endregion

    #region Alternative Action
    public virtual bool CanAlternativeAction(Vector2 position)
    {
        return true;
    }

    public virtual void OnAlternativeAction(Vector2 position)
    {
        if (baseProperty.AlternativeSound) PlayAlternativeSoundRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayAlternativeSoundRpc()
    {
        AudioElement.PlayOneShot(baseProperty.AlternativeSound);
    }
    #endregion

    #region Select On Client
    public virtual void OnItemSelectedOnClient()
    {

    }

    public virtual void OnItemDeselectedOnClient()
    {

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
