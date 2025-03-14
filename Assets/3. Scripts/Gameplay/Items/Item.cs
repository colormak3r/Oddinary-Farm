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
    public AudioSource AudioSource { get; private set; }

    public virtual void Initialize(ItemProperty baseProperty)
    {
        AudioSource = transform.root.GetComponent<AudioSource>();
        if (!AudioSource) Debug.LogError("AudioSource not found in parent object", this);

        ItemSystem = transform.root.GetComponent<ItemSystem>();
        if (!ItemSystem) Debug.LogError("ItemSystem not found in parent object", this);

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
        if (AudioSource == null) return;

        AudioSource.PlayOneShot(baseProperty.PrimarySound);
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
        AudioSource.PlayOneShot(baseProperty.SecondarySound);
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
        AudioSource.PlayOneShot(baseProperty.AlternativeSound);
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
