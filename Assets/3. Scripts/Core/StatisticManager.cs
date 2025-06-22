using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public enum StatisticType
{
    TimeDied,
    TimeSinceLastDeath,
    DistanceTravelled,
    ItemsUsed,
    ItemsCollected,
    EntitiesKilled,
    AnimalsCaptured,
    DamageTaken,
    DamageDealt
}

/// A generic bag-of-counters keyed by *TKey*
[Serializable]
public class CounterMap<TKey> where TKey : notnull
{
    [SerializeField] private Dictionary<TKey, ulong> counts = new();

    public void Add(TKey key, ulong amount = 1)
    {
        counts.TryGetValue(key, out var cur);
        counts[key] = cur + amount;
    }

    public void Reset() => counts.Clear();
    public IReadOnlyDictionary<TKey, ulong> Counts => counts;
}


[Serializable]
public class StatisticData
{
    // Scalar timers
    public ulong timeDied;
    public ulong timeSinceLastDeath;
    public ulong distanceTravelled;

    // All the collections are now one line each
    public CounterMap<ItemProperty> itemsUsed = new();
    public CounterMap<ItemProperty> itemsCollected = new();

    public CounterMap<string> entitiesKilled = new();   // key = entityName
    public CounterMap<string> animalsCaptured = new();
    public CounterMap<string> damageTaken = new();
    public CounterMap<string> damageDealt = new();

    public void Reset()
    {
        timeDied = distanceTravelled = timeSinceLastDeath = 0;
        entitiesKilled.Reset(); animalsCaptured.Reset();
        damageTaken.Reset(); damageDealt.Reset();
    }
}

public sealed class StatisticManager : MonoBehaviour
{
    public static StatisticManager Main { get; private set; }
    [Header("Settings")]
    [SerializeField]
    private float collectionInterval = 5f;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;

    private readonly StatisticData data = new();
    private float collectionStartTime = 0f; // Start time for the collection interval
    private bool isCollectingData = true; // Set to false to stop collecting data
    private float nextCollectionTime;
    private string filePath;
    private Dictionary<StatisticType, Action<string, ulong>> stringUpdaters;
    private Dictionary<StatisticType, Action<ItemProperty, ulong>> itemUpdaters;
    private List<StatisticSnapshot> statTimeline = new();

    public Action<StatisticData> OnDataUpdated;

    void Awake()
    {
        if (Main == null) Main = this; else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Build the tables once – O(1) look-ups afterwards
        stringUpdaters = new()
        {
            { StatisticType.EntitiesKilled,  (s,n) => data.entitiesKilled.  Add(s,n) },
            { StatisticType.AnimalsCaptured, (s,n) => data.animalsCaptured. Add(s,n) },
            { StatisticType.DamageTaken,     (s,n) => data.damageTaken.     Add(s,n) },
            { StatisticType.DamageDealt,     (s,n) => data.damageDealt.     Add(s,n) },
        };

        itemUpdaters = new()
        {
            { StatisticType.ItemsUsed,      (i,n) => data.itemsUsed.     Add(i,n) },
            { StatisticType.ItemsCollected, (i,n) => data.itemsCollected.Add(i,n) },
        };

        // TODO: change to Application.persistentDataPath later
        // Define base directory
        string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ODD_STATS");
        Directory.CreateDirectory(baseDirectory);

        // Then build the final file path
        filePath = GetFilePath(baseDirectory);
    }

    private void Update()
    {
        if (isCollectingData && Time.time > nextCollectionTime)
        {
            nextCollectionTime = Time.time + collectionInterval;
            RecordData();
        }
    }

    private void OnDestroy()
    {
        // Ensure we write the final data before destroying the object
        if (isCollectingData)
        {
            WriteData();
            isCollectingData = false;
        }
    }

    #region Update Stat

    public void UpdateStat(StatisticType type, ulong value)
    {
        switch (type)
        {
            case StatisticType.TimeDied: data.timeDied = value; break;
            case StatisticType.DistanceTravelled: data.distanceTravelled = value; break;
            case StatisticType.TimeSinceLastDeath: data.timeSinceLastDeath = value; break;
            default: Warn(type); break;
        }

        OnDataUpdated?.Invoke(data);
        StatUI.Main.UpdateStat(data);
    }

    public void UpdateStat(StatisticType type, string key, ulong value = 1)
    {
        if (!stringUpdaters.TryGetValue(type, out var fn)) { Warn(type); return; }
        fn(key.Replace(" Variant", "").Replace("(Clone)", ""), value);

        OnDataUpdated?.Invoke(data);
        StatUI.Main.UpdateStat(data);
    }

    public void UpdateStat(StatisticType type, ItemProperty key, ulong value = 1)
    {
        if (!itemUpdaters.TryGetValue(type, out var fn)) { Warn(type); return; }
        fn(key, value);

        OnDataUpdated?.Invoke(data);
        StatUI.Main.UpdateStat(data);
    }

    private void Warn(StatisticType t)
    {
        if (showDebugs) Debug.LogWarning($"Unsupported StatisticType {t}");
    }

    #endregion

    public void Initialize()
    {
        ResetCurrentData();
        collectionStartTime = Time.time;
    }

    private void ResetCurrentData()
    {
        data.Reset();
    }

    private void RecordData()
    {
        statTimeline.Add(data.ToSnapshot(Time.time - collectionStartTime));
    }

    private void WriteData()
    {
        // Write the data to a CSV file
        // https://dev.socrata.com/docs/formats/csv
        if (statTimeline.Count == 0) return;

        // Step 1: Collect all unique keys (columns)
        var allKeys = new HashSet<string>();
        foreach (var snapshot in statTimeline)
            foreach (var kv in snapshot.flatStats)
                allKeys.Add(kv.Key);

        var orderedKeys = allKeys.OrderBy(k => k).ToList(); // consistent column order

        // Step 2: Build CSV
        var sb = new StringBuilder();

        // Header row
        sb.Append("timestamp");
        foreach (var key in orderedKeys)
            sb.Append($",{key}");
        sb.AppendLine();

        // Data rows
        foreach (var snapshot in statTimeline)
        {
            sb.Append(snapshot.timestamp.ToString("F2"));
            foreach (var key in orderedKeys)
            {
                snapshot.flatStats.TryGetValue(key, out var value);
                sb.Append($",{value}");
            }
            sb.AppendLine();
        }

        // Step 3: Write to file
        File.WriteAllText(filePath, sb.ToString());
        if (showDebugs)
            Debug.Log($"Statistics exported to: {filePath}");
    }


    #region Utility

    private string GetFilePath(string directory)
    {
        DateTime now = DateTime.Now;
        string time = now.ToString("MMddyy-HHmmss");
        string randomNumber = UnityEngine.Random.Range(1000, 9999).ToString();
        return directory + "/stat-" + time + "-" + randomNumber + "-" + VersionUtility.VERSION + ".csv";
    }

    #endregion
}

[Serializable]
public class StatisticSnapshot
{
    public float timestamp; // or use DateTime for real-world time
    public Dictionary<string, ulong> flatStats = new();
}

public static class StatisticDataExtensions
{
    public static StatisticSnapshot ToSnapshot(this StatisticData data, float timestamp)
    {
        var snap = new StatisticSnapshot { timestamp = timestamp };

        void Add(string key, ulong value)
        {
            snap.flatStats[key] = value;
        }

        Add("timeDied", data.timeDied);
        Add("distanceTravelled", data.distanceTravelled);
        Add("timeSinceLastDeath", data.timeSinceLastDeath);

        foreach (var kv in data.itemsUsed.Counts) Add($"itemsUsed:{kv.Key.name}", kv.Value);
        foreach (var kv in data.itemsCollected.Counts) Add($"itemsCollected:{kv.Key.name}", kv.Value);
        foreach (var kv in data.entitiesKilled.Counts) Add($"entitiesKilled:{kv.Key}", kv.Value);
        foreach (var kv in data.animalsCaptured.Counts) Add($"animalsCaptured:{kv.Key}", kv.Value);
        foreach (var kv in data.damageTaken.Counts) Add($"damageTaken:{kv.Key}", kv.Value);
        foreach (var kv in data.damageDealt.Counts) Add($"damageDealt:{kv.Key}", kv.Value);

        return snap;
    }
}
