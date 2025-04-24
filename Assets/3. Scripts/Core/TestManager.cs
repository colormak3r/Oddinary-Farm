using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum TestPreset
{
    CornFarmDemo,
}

public class TestManager : NetworkBehaviour
{
    public static TestManager Main { get; private set; }

    [Header("Settings")]
    [SerializeField]
    private TestPreset testPreset;

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
}
