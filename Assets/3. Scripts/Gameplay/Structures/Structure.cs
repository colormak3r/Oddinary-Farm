using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Structure : NetworkBehaviour
{
    [Header("Structure Settings")]
    [SerializeField]
    private bool isMovable = true;
    [SerializeField]
    private bool isRemovable = true;
    [SerializeField]
    private StructureProperty property;
    public StructureProperty Property => property;

    private StructureNetworkTransform networkTransform;

    protected virtual void Awake()
    {
        networkTransform = GetComponent<StructureNetworkTransform>();
    }

    public void MoveTo(Vector2 position)
    {
        if (!isMovable) return;

        if (networkTransform != null)
            networkTransform.MoveTo(position);
        else
            MoveToRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void MoveToRpc(Vector2 position)
    {
        transform.position = position;
    }

    public void RemoveStructure()
    {
        if (!isRemovable) return;
        RemoveRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void RemoveRpc()
    {
        if (IsServer)
        {
            RemoveOnServer();
        }

        StartCoroutine(RemoveCoroutine());
    }

    private IEnumerator RemoveCoroutine()
    {
        yield return new WaitUntil(() => !NetworkObject.IsSpawned);
        RemoveOnClient();
    }

    protected virtual void RemoveOnClient()
    {
        Destroy(gameObject);
    }

    protected virtual void RemoveOnServer()
    {
        AssetManager.Main.SpawnItem(property.StructureItemProperty, transform.position);
        NetworkObject.Despawn(false);
    }
}
