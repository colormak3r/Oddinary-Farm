using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Main;

    [Header("Settings")]
    [SerializeField]
    private bool transitionOnStart = true;

    [Header("Gameover Settings")]
    [SerializeField]
    private bool canGameOver = true;

    [Header("Debugs")]
    [SerializeField]
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool isInitializing;
    public bool IsInitializing => isInitializing;
    [SerializeField]
    private bool isGameOver;
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Main == null)
            Main = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        TimeManager.Main.OnHourChanged.AddListener(OnHourChanged);
        AudioManager.Main.OnNetworkSpawn();
        Initialize();
    }

    public override void OnNetworkDespawn()
    {
        TimeManager.Main.OnHourChanged.RemoveListener(OnHourChanged);
        AudioManager.Main.OnNetworkDespawn();
    }

    private void OnHourChanged(int currentHour)
    {
        if (!canGameOver) return;

        if (FloodManager.Main.CurrentFloodLevelValue >= WorldGenerator.Main.HighestElevation)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            var escaped = localPlayer.GetComponent<HotAirBalloonController>().IsControlledValue;
            localPlayer.GetComponent<ControllableController>().SetControl(false);
            GameOver(escaped);
        }
    }

    private void Initialize()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        if (isInitializing || isInitialized) yield break;
        isInitializing = true;

        if (transitionOnStart)
            TransitionUI.Main.ShowNoFade();
        else
            TransitionUI.Main.HideNoFade();

        StatisticsManager.Main.Initialize();
        yield return WorldGenerator.Main.Initialize();
        if (IsServer) yield return ScenarioManager.Main.RunTestPresetCoroutine();

        if (TransitionUI.Main.IsShowing) yield return TransitionUI.Main.HideCoroutine();
        if (!TutorialUI.Main.DontShowAgain) yield return TutorialUI.Main.ShowCoroutine();

        yield return MapUI.Main.ShowCoroutine();

        isInitializing = false;
        isInitialized = true;
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    private IEnumerator ReturnToMainMenuCoroutine()
    {
        yield return OptionsUI.Main.HideCoroutine();

        ConnectionManager.Main.Disconnect(false);
    }

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
        StartCoroutine(ReturnToMainMenuCoroutine());
    }

    #endregion
}
