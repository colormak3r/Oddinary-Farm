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

    /// <summary>
    /// Called by the WorldGenerator to unload this object. 
    /// The object will be CLEANED UP by the WorldGenerator when it goes out of view, 
    /// and it WILL be spawned again through procedural generation.
    /// </summary>
    public virtual void UnloadOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        if (gameObject) Destroy(gameObject);
    }

    /// <summary>
    /// Called when the object needs to be despawned with custom logic. 
    /// The object will NOT be cleaned up by the WorldGenerator when out of view, 
    /// and it will NOT be spawned again through procedural generation.
    /// </summary>
    public virtual void DespawnOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        status.isObservable = false;
        // Destroy logic should be handled by Entity Status or caller
    }

    /// <summary>
    /// Called when the object should remain in the world for now, but no longer be observable. 
    /// The object will be cleaned up by the WorldGenerator when it goes out of view, 
    /// and it will NOT be spawned again through procedural generation.
    /// </summary>
    public virtual void EndObservabilityOnServer()
    {
        status.isObservable = false;
    }
}
