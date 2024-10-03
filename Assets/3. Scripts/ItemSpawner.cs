using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField]
    private int spawnIndex;
    [SerializeField]
    private GameObject itemPrefab;
    [SerializeField]
    private ItemProperty[] itemProperties;

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        var randomPos = 2 * (Vector2)Random.onUnitSphere;
        GameObject go = Instantiate(itemPrefab, transform.position + (Vector3)randomPos, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var itemReplica = go.GetComponent<ItemReplica>();
        itemReplica.SetProperty(itemProperties[spawnIndex]);
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        foreach(var property in itemProperties)
        {
            var randomPos = 2 * (Vector2)Random.onUnitSphere;
            GameObject go = Instantiate(itemPrefab, transform.position + (Vector3)randomPos, Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            var itemReplica = go.GetComponent<ItemReplica>();
            itemReplica.SetProperty(property);
        }
    }

    private void OnGUI()
    {
        if(!NetworkManager.Singleton.IsHost) return;
        // Button size
        int buttonWidth = 100;
        int buttonHeight = 30;

        // Calculate position for top-right alignment
        float xPosition = Screen.width - buttonWidth - 10;  // 10px padding from the right
        float yPosition = 10;  // 10px padding from the top

        // Create button in the top-right corner
        if (GUI.Button(new Rect(xPosition, yPosition, buttonWidth, buttonHeight), "Spawn All"))
        {
            SpawnAll();
        }
    }
}
