using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpawnerProperty : ItemProperty
{
    [Header("Spawner Settings")]
    [SerializeField]
    private GameObject prefabToSpawn;
    [SerializeField]
    private Sprite iconSprite;
    [SerializeField]
    private int size = 1;

    public GameObject PrefabToSpawn => prefabToSpawn;
    public Sprite IconSprite => iconSprite;
    public int Size => size;
}
