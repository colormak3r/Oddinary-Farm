using System.Collections;
using System.Collections.Generic;
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
