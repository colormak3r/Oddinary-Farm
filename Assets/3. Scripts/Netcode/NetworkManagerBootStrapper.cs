using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerBootStrapper : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private GameObject networkManagerObject;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("NetworkManager is null, starting up the scene-placed one", networkManagerObject);
            networkManagerObject.SetActive(true);
        }
    }
}
