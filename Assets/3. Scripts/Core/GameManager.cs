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

        //yield return WorldGenerator.Main.BuildWorld();
        yield return TransitionUI.Main.HideCoroutine();

        if (TimeManager.Main.IsDay)
            AudioManager.Main.PlayAmbientSound(AmbientTrack.Day);
        else
            AudioManager.Main.PlayAmbientSound(AmbientTrack.Night);

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
}
