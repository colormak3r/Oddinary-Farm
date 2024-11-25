using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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
            Destroy(gameObject);
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

        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);
        yield return WorldGenerator.Main.GenerateTerrainCoroutine(Vector2.zero);
        yield return TransitionUI.Main.HideCoroutine();
        yield return MapUI.Main.ShowCoroutine();

        isInitializing = false;
        isInitialized = true;
    }
}
