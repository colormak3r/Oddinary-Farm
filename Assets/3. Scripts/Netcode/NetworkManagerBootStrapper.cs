/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/03/2025 (Khoa)
 * Notes:           Used to make sure at least one NetworkManager is active in the scene.
*/

using Unity.Netcode;
using UnityEngine;

public class NetworkManagerBootStrapper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private GameObject networkManagerObject;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            if(showDebugs) Debug.Log("NetworkManager is null, starting up the scene-placed one", networkManagerObject);
            networkManagerObject.SetActive(true);
        }
    }
}
