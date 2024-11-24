using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    public void Spawn(ItemProperty itemProperty, Vector2 position, float randomRange = 2f, bool randomForce = true)
    {
        SpawnItemRpc(itemProperty, position, randomRange, randomForce);
    }

    [Rpc(SendTo.Server)]
    private void SpawnItemRpc(ItemProperty itemProperty, Vector2 position, float randomRange, bool randomForce)
    {
        var randomPos = randomRange * (Vector2)Random.onUnitSphere;
        position = position == default ? transform.position + (Vector3)randomPos : position + randomPos;
        GameObject go = Instantiate(AssetManager.Main.ItemReplicaPrefab, position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        var itemReplica = go.GetComponent<ItemReplica>();
        itemReplica.SetProperty(itemProperty);
        itemReplica.AddRandomForce();
    }

    /* [Header("Settings")]
     [SerializeField]
     private static bool showGizmos;
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
     }*/
}
