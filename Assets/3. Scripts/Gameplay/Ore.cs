using Unity.Netcode;
using UnityEngine;

public class Ore : NetworkBehaviour
{
    protected override void OnNetworkPostSpawn()
    {
        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                WorldGenerator.Main.RemoveFoliageOnClient(transform.position + new Vector3(i, j));
            }
        }
    }
}