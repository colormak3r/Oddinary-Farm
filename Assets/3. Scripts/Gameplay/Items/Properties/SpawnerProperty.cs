using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnerProperty : ToolProperty
{
    [Header("Spawner Settings")]
    [SerializeField]
    private GameObject prefabToSpawn;
    [SerializeField]
    private Vector2 spawnOffset;


    public GameObject PrefabToSpawn => prefabToSpawn;
    public Vector2 SpawnOffset => spawnOffset;
    public virtual ScriptableObject InitScript => null;
}
