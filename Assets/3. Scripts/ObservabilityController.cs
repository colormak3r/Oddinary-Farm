using UnityEngine;

public class ObservabilityController : MonoBehaviour
{
    private ObservablePrefabStatus status;

    public void InitializeOnServer(ObservablePrefabStatus status)
    {
        this.status = status;
        status.controller = this;
    }

    public void DespawnOnServer()
    {
        status.isSpawned = false;
        status.controller = null;
        Destroy(gameObject);
    }

    public void EndObservabilityOnServer()
    {
        status.isObservable = false;
        DespawnOnServer();
    }
}
