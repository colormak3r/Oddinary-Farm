/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum ScenarioPreset
{
    None,
    CornFarmDemo,
    MidSizeFarmDemo,
    ChickenFarmDemo,
    DefenseDemo,
}

public class ScenarioManager : NetworkBehaviour
{
    public static ScenarioManager Main { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private NetworkVariable<ScenarioPreset> CurrentScenario = new NetworkVariable<ScenarioPreset>();

    [Header("Asset Spawn Preset")]
    [SerializeField]
    private AssetSpawnPreset cornFarmDemoPreset;
    [SerializeField]
    private AssetSpawnPreset midSizeFarmDemoPreset;
    [SerializeField]
    private AssetSpawnPreset chickenFarmDemoPreset;
    [SerializeField]
    private AssetSpawnPreset defenseDemoPreset;

    [Header("Temporary Settings")]
    [SerializeField]
    private bool overrideSettings = false;
    public bool OverrideSettings => overrideSettings;
    [SerializeField]
    private bool canSpawnEnemies = false;
    [SerializeField]
    private bool canSpawnResources = false;
    public bool CanSpawnResources => overrideSettings & canSpawnResources;
    [SerializeField]
    private uint startingCoins = 10;
    [SerializeField]
    private float realMinutesPerInGameDay = 10f;
    [SerializeField]
    private int dayOffset = 1;
    [SerializeField]
    private int hourOffset = 19;
    [SerializeField]
    private int minuteOffset = 50;

    [Header("Settings")]
    [SerializeField]
    private BlueprintProperty woodenFence;
    [SerializeField]
    private SeedProperty cornSeed;

    private LayerManager layerManager;
    private ItemSystem itemSystem;

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
        DontDestroyOnLoad(gameObject);

        itemSystem = GetComponent<ItemSystem>();
    }

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer & overrideSettings)
        {
            StartCoroutine(InitializeCoroutine());
        }

        StartCoroutine(WaitForPlayerObjectCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() => WorldGenerator.Main.IsInitialized);
        WalletManager.Main.AddToWallet(startingCoins);
        TimeManager.Main.SetRealMinutesPerDay(realMinutesPerInGameDay);
        TimeManager.Main.SetTimeOffset(dayOffset, hourOffset, minuteOffset);

        yield return new WaitUntil(() => CreatureSpawnManager.Main.IsInitialized);
        CreatureSpawnManager.Main.SetCanSpawn(canSpawnEnemies);
    }

    private IEnumerator WaitForPlayerObjectCoroutine()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.LocalClient.PlayerObject != null);

        if (CurrentScenario.Value == ScenarioPreset.None) yield break;

        var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
        var inventory = playerObject.GetComponent<PlayerInventory>();
        var preset = GetAssetSpawnPreset(CurrentScenario.Value);

        // Add starting coins to the player's inventory
        inventory.AddCoinsOnClient(preset.StartingWallet);

        // Add starting items to the player's inventory
        foreach (var item in preset.StartingItems)
        {
            inventory.AddItem(item);
        }
    }

    public IEnumerator RunTestPresetCoroutine()
    {
        layerManager = LayerManager.Main;

        switch (CurrentScenario.Value)
        {
            case ScenarioPreset.CornFarmDemo:
                yield return CornFarmDemo();
                break;
            case ScenarioPreset.MidSizeFarmDemo:
                yield return SpawnAssetPreset(midSizeFarmDemoPreset);
                break;
            case ScenarioPreset.ChickenFarmDemo:
                yield return SpawnAssetPreset(chickenFarmDemoPreset);
                break;
            case ScenarioPreset.DefenseDemo:
                yield return SpawnAssetPreset(defenseDemoPreset);
                break;
            case ScenarioPreset.None:
                break;
        }
    }

    private IEnumerator CornFarmDemo()
    {
        var boundryX = new Vector2Int(-20, -2);
        var boundryY = new Vector2Int(-5, 5);

        for (int x = boundryX.x; x < boundryX.y; x++)
        {
            for (int y = boundryY.x; y < boundryY.y; y++)
            {
                if (x == boundryX.x || x == boundryX.y - 1 || y == boundryY.x || y == boundryY.y - 1)
                {
                    itemSystem.Spawn(new Vector2(x, y), woodenFence);
                }
                else
                {
                    var pos = new Vector2(x, y);
                    itemSystem.SpawnFarmPlot(pos);
                    itemSystem.Spawn(pos, cornSeed);
                    var plantHit = Physics2D.OverlapPoint(pos, layerManager.PlantLayer);
                    while (plantHit == null)
                    {
                        plantHit = Physics2D.OverlapPoint(pos, layerManager.PlantLayer);
                        yield return null;
                    }
                    plantHit.GetComponent<Plant>().FullyGrown();
                }

                yield return null;
            }
        }
    }

    private IEnumerator SpawnAssetPreset(AssetSpawnPreset assetSpawnPreset)
    {
        var spawnPoint = WorldGenerator.Main.HighestElevationPoint - new Vector2(3, 0);

        foreach (var prefabPosition in assetSpawnPreset.PrefabPositions)
        {
            var position = prefabPosition.Position + spawnPoint;
            if (prefabPosition.Prefab == AssetManager.Main.FarmPlotPrefab)
            {
                itemSystem.SpawnFarmPlot(position);
            }
            else
            {
                AssetManager.Main.SpawnPrefabOnServer(prefabPosition.Prefab, position);
            }

            DestroyResource(position);
            yield return null;
        }

        foreach (var prefabPosition in assetSpawnPreset.SpawnerPositions)
        {
            var position = prefabPosition.Position + spawnPoint;
            itemSystem.Spawn(position, prefabPosition.SpawnerProperty);
            DestroyResource(position);
            yield return null;
        }

        yield return null;
    }

    private void DestroyResource(Vector2 position)
    {
        var hit = Physics2D.OverlapPoint(position, layerManager.ResourceLayer);
        if (hit != null)
        {
            Destroy(hit.gameObject);
        }
    }

    public void SetScenario(ScenarioPreset scenario)
    {
        CurrentScenario.Value = scenario;
    }

    private AssetSpawnPreset GetAssetSpawnPreset(ScenarioPreset scenario)
    {
        return scenario switch
        {
            ScenarioPreset.CornFarmDemo => cornFarmDemoPreset,
            ScenarioPreset.MidSizeFarmDemo => midSizeFarmDemoPreset,
            ScenarioPreset.ChickenFarmDemo => chickenFarmDemoPreset,
            ScenarioPreset.DefenseDemo => defenseDemoPreset,
            _ => LogAndReturnNull(scenario)
        };
    }

    private AssetSpawnPreset LogAndReturnNull(ScenarioPreset scenario)
    {
        Debug.LogError($"Unhandled ScenarioPreset: {scenario}");
        return null;
    }
}