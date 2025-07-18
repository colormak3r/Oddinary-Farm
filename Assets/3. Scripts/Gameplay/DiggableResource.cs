using Unity.Netcode;
using UnityEngine;

public class DiggableResource : NetworkBehaviour, IDiggable
{
    [SerializeField]
    private ItemProperty lootProperty;

    public void Dig(Transform source)
    {
        DigRpc(source.gameObject);
    }

    [Rpc(SendTo.Server)]
    private void DigRpc(NetworkObjectReference sourceRef)
    {
        AssetManager.Main.SpawnItem(lootProperty, transform.position, sourceRef);
        Destroy(gameObject);
    }
}
