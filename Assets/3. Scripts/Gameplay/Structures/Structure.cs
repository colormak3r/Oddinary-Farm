/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/24/2025 (Khoa)
 * Notes:           <write here>
*/

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

    #region Move To
    // TODO: Handle spriteblender reblending when moving
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
    #endregion

    #region Remove Structure
    /// <summary>
    /// Removes the structure from the game. Should be triggered by the player ItemSystem 
    /// only since this will drop the structure's blueprint.
    /// </summary>
    public void RemoveStructure()
    {
        if (!isRemovable) return;
        RemoveRpc();
    }

    [Rpc(SendTo.Server)]
    private void RemoveRpc()
    {
        AssetManager.Main.SpawnItem(property.BlueprintProperty, transform.position);
        Destroy(gameObject);
    }
    #endregion
}
