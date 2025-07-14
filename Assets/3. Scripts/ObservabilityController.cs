/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/03/2025 
 * Last Modified:   07/03/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEngine;

public class ObservabilityController : MonoBehaviour
{
    private ObservablePrefabStatus status;

    public void InitializeOnServer(ObservablePrefabStatus status)
    {
        this.status = status;
        status.controller = this;
    }

    // Used by the WorldGenerator to unload only
    public void UnloadOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        if (gameObject) Destroy(gameObject);
    }

    // Used by EntityStatus on death to despawn only
    public void DespawnOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        status.isObservable = false;
        // Use entity on death logic instead
    }

    // Used when object doesn't need to get destroyed right away
    public void EndObservabilityOnServer()
    {
        status.isObservable = false;
    }
}
