using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnerProperty : ItemProperty
{
    [Header("Spawner Settings")]
    [SerializeField]
    private GameObject prefabToSpawn;
    
    public GameObject PrefabToSpawn => prefabToSpawn;
}
