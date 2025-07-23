/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/03/2025 
 * Last Modified:   07/03/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class ObservabilityController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2 spawnOffset;
    public Vector2 SpawnOffset => spawnOffset;

    private ObservablePrefabStatus status;

    public virtual void InitializeOnServer(ObservablePrefabStatus status)
    {
        this.status = status;
        status.controller = this;
    }

    // Used by the WorldGenerator to unload only
    public virtual void UnloadOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        if (gameObject) Destroy(gameObject);
    }

    // Used by EntityStatus on death to despawn only
    public virtual void DespawnOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        status.isObservable = false;
        // Use entity on death logic instead
    }

    // Used when object doesn't need to get destroyed right away
    public virtual void EndObservabilityOnServer()
    {
        status.isObservable = false;
    }
}
