/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   08/07/2025 (Khoa)
 * Notes:           <write here>
*/

using System;
using Unity.Netcode;
using UnityEngine;

public class Ore : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    [Range(0.01f, 1.0f)]
    private float crystalSnailChance = 0.05f; // 20% chance to spawn a Crystal Snail when ore is collected
    [SerializeField]
    private GameObject crystalSnailPrefab;

    private EntityStatus entityStatus;

    private void Awake()
    {
        entityStatus = GetComponent<EntityStatus>();
    }

    protected override void OnNetworkPostSpawn()
    {
        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                WorldGenerator.Main.RemoveFoliageOnClient(transform.position + new Vector3(i, j));
            }
        }

        if (IsServer) entityStatus.OnDeathOnServer.AddListener(HandleOnOreDestroyed);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) entityStatus.OnDeathOnServer.RemoveListener(HandleOnOreDestroyed);
    }

    private void HandleOnOreDestroyed()
    {
        if (UnityEngine.Random.value < crystalSnailChance)
        {
            var crystalSnail = Instantiate(crystalSnailPrefab, transform.position, Quaternion.identity);
            crystalSnail.GetComponent<NetworkObject>().Spawn();
        }
    }
}