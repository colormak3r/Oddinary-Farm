using Unity.Netcode;
using UnityEngine;

public class Spectator : MonoBehaviour
{
    public static Spectator Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCamera(ulong id)
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
        if (id > (ulong)connectedClients.Count)
        {
            CinemachineManager.Main.Camera.Follow = transform;
        }
        else
        {
            foreach (var client in connectedClients)
            {
                if (client.ClientId == id)
                {
                    CinemachineManager.Main.Camera.Follow = client.PlayerObject.transform;
                    return;
                }
            }
            Debug.LogError($"Cannot find SpectateId: {id}");
        }
    }

    public void SetCamera(Transform transform)
    {
        CinemachineManager.Main.Camera.Follow = transform;
    }

    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }
}
