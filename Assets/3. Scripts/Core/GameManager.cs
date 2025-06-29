using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Main;

    [Header("Settings")]
    [SerializeField]
    private bool transitionOnStart = true;
    [SerializeField]
    private GameObject playerPrefab;
    [SerializeField]
    private GameObject spawnPointPrefab;
    [SerializeField]
    private Vector3 spawnPointOffset = new Vector2(0, -1f);

    [Header("Gameover Settings")]
    [SerializeField]
    private bool canGameOver = true;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool isGameOver;
    public bool IsGameOver => isGameOver;

    private NetworkVariable<NetworkObjectReference> SpawnPointObjectRef = new NetworkVariable<NetworkObjectReference>();
    public Vector2 SpawnPoint => SpawnPointObjectRef.Value.TryGet(out var spawnPointNetObj) ? spawnPointNetObj.transform.position + spawnPointOffset : Vector2.zero;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
        AudioManager.Main.OnNetworkSpawn();
        Initialize();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
        AudioManager.Main.OnNetworkDespawn();
    }

    private void OnHourChanged(int currentHour)
    {
        if (!canGameOver) return;

        // Check if the flood level is at or above the highest elevation
        if (FloodManager.Main.CurrentFloodLevelValue >= WorldGenerator.Main.HighestElevationValue)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            var escaped = localPlayer.GetComponent<HotAirBalloonController>().IsControlledValue;
            localPlayer.GetComponent<ControllableController>().SetControl(false);
            GameOver(escaped);
        }
    }

    private void OnClientConnected(ulong uid)
    {
        if (showDebugs) Debug.Log($"Client {uid} connected. Initializing game for the client.");

        StartCoroutine(SpawnPlayerCoroutine(uid));
    }

    private IEnumerator SpawnPlayerCoroutine(ulong uid)
    {
        yield return new WaitUntil(() => isInitialized);

        if (showDebugs) Debug.Log($"Spawning player for client {uid} at spawn point.");

        // Instantiate the player prefab at the spawn point
        var playerObj = Instantiate(playerPrefab, SpawnPoint, Quaternion.identity);
        playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(uid);
    }

    #region Initialization

    private void Initialize()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        if (isInitialized) yield break;

        // Show transition UI if configured to do so
        if (transitionOnStart)
            TransitionUI.Main.ShowNoFade();
        else
            TransitionUI.Main.HideNoFade();

        // All clients initialize these on their own
        StatisticsManager.Main.Initialize();
        HeatMapManager.Main.Initialize(WorldGenerator.Main.MapSize);

        // Initialize WolrdGenerator: generates the world and build the visible chunks 
        yield return WorldGenerator.Main.Initialize();

        // Initialize spawn object
        // Spawn point is offset from spawn object
        NetworkObject spawnPointNetObj;
        if (IsServer)
        {
            var spawnPointObj = Instantiate(spawnPointPrefab, WorldGenerator.Main.HighestElevationPoint, Quaternion.identity);
            spawnPointNetObj = spawnPointObj.GetComponent<NetworkObject>();
            spawnPointNetObj.Spawn();
            SpawnPointObjectRef.Value = spawnPointNetObj;
        }

        while (!SpawnPointObjectRef.Value.TryGet(out spawnPointNetObj))
        {
            yield return null; // Wait until the spawn point is available
        }

        CinemachineManager.Main.Camera.ForceCameraPosition(
            spawnPointNetObj.transform.position + spawnPointOffset,
            Quaternion.identity
        );

        // Build the scenario preset if it's a server
        if (IsServer) yield return ScenarioManager.Main.RunTestPresetCoroutine();

        isInitialized = true;
        // The player can take control after this
        // Many Singleton waited for the game manager to be initialzied first
        //  and will be initialized after this

        // Hide the transition UI after the world is generated
        if (TransitionUI.Main.IsShowing) yield return TransitionUI.Main.HideCoroutine();

        // All clients will show the TutorialUI on their own
        if (!TutorialUI.Main.DontShowAgain) StartCoroutine(TutorialUI.Main.ShowCoroutine());

        StartCoroutine(MapUI.Main.ShowCoroutine());
    }

    #endregion

    #region Game Over

    private Coroutine gameOverCoroutine;
    public void GameOver(bool escaped)
    {
        if (gameOverCoroutine != null) return;
        isGameOver = true;
        Debug.Log($"Game Over: {escaped}");
        GameOverUI.Main.SetGameoverText(escaped);
        gameOverCoroutine = StartCoroutine(GameOverCoroutine());
    }

    public IEnumerator GameOverCoroutine()
    {
        yield return OptionsUI.Main.HideCoroutine();
        yield return GameOverUI.Main.ShowCoroutine();
        yield return new WaitForSeconds(5f);
        yield return TransitionUI.Main.ShowCoroutine();
        // TODO: Show game over screen with stats and options to return to main menu or restart the game
        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    #endregion

    #region Leave/Return to Main Menu

    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    private IEnumerator ReturnToMainMenuCoroutine()
    {
        yield return OptionsUI.Main.HideCoroutine();

        ConnectionManager.Main.Disconnect(false);
    }

    #endregion
}
