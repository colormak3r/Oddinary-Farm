using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum TestPreset
{
    None,
    CornFarmDemo,
    MidSizeFarmDemo,
}

public class TestManager : NetworkBehaviour
{
    public static TestManager Main { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private TestPreset testPreset;

    [Header("Asset Spawn Preset")]
    [SerializeField]
    private AssetSpawnPreset cornFarmDemoPreset;
    [SerializeField]
    private AssetSpawnPreset midSizeFarmDemoPreset;

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

        itemSystem = GetComponent<ItemSystem>();
    }

    private void Start()
    {
        layerManager = LayerManager.Main;
    }

    public IEnumerator RunTestPresetCoroutine()
    {
        switch (testPreset)
        {
            case TestPreset.CornFarmDemo:
                yield return CornFarmDemo();
                break;
            case TestPreset.MidSizeFarmDemo:
                yield return MidSizeFarmDemo();
                break;
            case TestPreset.None:
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

    private IEnumerator MidSizeFarmDemo()
    {
        foreach (var prefabPosition in midSizeFarmDemoPreset.PrefabPositions)
        {
            if (prefabPosition.Prefab == AssetManager.Main.FarmPlotPrefab)
            {
                itemSystem.SpawnFarmPlot(prefabPosition.Position);
            }
            else
            {
                AssetManager.Main.SpawnPrefabOnServer(prefabPosition.Prefab, prefabPosition.Position);
            }

            DestroyResource(prefabPosition.Position);
            yield return null;
        }

        foreach (var prefabPosition in midSizeFarmDemoPreset.SpawnerPositions)
        {
            itemSystem.Spawn(prefabPosition.Position, prefabPosition.SpawnerProperty);
            DestroyResource(prefabPosition.Position);
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
}