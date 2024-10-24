using Unity.Netcode;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private GameObject spiderPrefab; // Spider prefab
    [SerializeField]
    private GameObject snailPrefab;  // Snail prefab
    private GameObject selectedPrefab; // The currently selected prefab

    [SerializeField]
    private Vector2 spawnPosition;
    [SerializeField]
    private int spawnCount;
    [SerializeField]
    private bool spawnOnInterval;
    [SerializeField]
    private float spawnInterval = 3f;

    private float nextSpawn;

    private void Start()
    {
        // Default to spiderPrefab at the start
        selectedPrefab = spiderPrefab;
    }

    private void Update()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (spawnOnInterval)
        {
            if (Time.time > nextSpawn)
            {
                nextSpawn = Time.time + spawnInterval;
                Spawn();
            }
        }
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        SpawnItemRpc(spawnPosition, spawnCount);
    }

    public void Spawn(Vector2 position = default, int spawnCount = 1)
    {
        SpawnItemRpc(position, spawnCount);
    }

    [Rpc(SendTo.Server)]
    private void SpawnItemRpc(Vector2 position, int spawnCount)
    {
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject go = Instantiate(selectedPrefab, position, Quaternion.identity); // Use the selected prefab
            go.GetComponent<NetworkObject>().Spawn();
        }
    }

    private void OnGUI()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Button size and padding
        int buttonWidth = 100;
        int buttonHeight = 30;
        float padding = 10f;

        // Calculate position for top-right alignment
        float xPosition = Screen.width - buttonWidth - padding;
        float yPosition = padding;

        // Create buttons for selecting Spider or Snail prefab
        if (GUI.Button(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spider"))
        {
            selectedPrefab = spiderPrefab;
        }

        yPosition += buttonHeight + padding;

        if (GUI.Button(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Snail"))
        {
            selectedPrefab = snailPrefab;
        }

        yPosition += buttonHeight + padding;

        // Create button to spawn the selected prefab
        if (GUI.Button(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spawn"))
        {
            Spawn();
        }

        // Display and modify spawnPosition
        yPosition += buttonHeight + padding;
        GUI.Label(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spawn Position:");
        spawnPosition.x = float.Parse(GUI.TextField(new Rect(xPosition, yPosition + buttonHeight, buttonWidth / 2, buttonHeight), spawnPosition.x.ToString()));
        spawnPosition.y = float.Parse(GUI.TextField(new Rect(xPosition + buttonWidth / 2, yPosition + buttonHeight, buttonWidth / 2, buttonHeight), spawnPosition.y.ToString()));

        // Display and modify spawnCount
        yPosition += buttonHeight * 2 + padding;
        GUI.Label(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spawn Count:");
        spawnCount = int.Parse(GUI.TextField(new Rect(xPosition, yPosition + buttonHeight, buttonWidth, buttonHeight), spawnCount.ToString()));

        // Display and modify spawnOnInterval
        yPosition += buttonHeight * 2 + padding;
        spawnOnInterval = GUI.Toggle(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), spawnOnInterval, "Spawn on Interval");

        // Display and modify spawnInterval if enabled
        if (spawnOnInterval)
        {
            yPosition += buttonHeight + padding;
            GUI.Label(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spawn Interval:");
            spawnInterval = float.Parse(GUI.TextField(new Rect(xPosition, yPosition + buttonHeight, buttonWidth, buttonHeight), spawnInterval.ToString()));
        }
    }
}
