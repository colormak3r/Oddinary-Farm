using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Main;

    [Header("Settings")]
    [SerializeField]
    private GameObject playerPrefab;

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized;
    [SerializeField]
    private bool isInitializing;

    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(Main.gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Initialize()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        if (isInitializing || isInitialized) yield break;
        isInitializing = true;

        yield return TransitionUI.Main.ShowCoroutine();
        yield return MapGenerator.Main.GenerateTerrainCoroutine(Vector2.zero);
        yield return TransitionUI.Main.UnShowCoroutine();

        isInitializing = false;
        isInitialized = true;
    }
}