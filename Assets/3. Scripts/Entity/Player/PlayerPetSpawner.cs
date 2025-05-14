using Unity.Netcode;
using UnityEngine;

public class PlayerPetSpawner : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool canSpawnPet = true;
    [SerializeField]
    private GameObject[] pets;

    protected override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        if (IsServer && canSpawnPet)
        {
            SpawnPetOnServer();
        }
    }

    private void SpawnPetOnServer()
    {
        var rigidbody2D = GetComponent<Rigidbody2D>();
        foreach (var pet in pets)
        {
            if (pet != null)
            {
                var petInstance = Instantiate(pet, transform.position, Quaternion.identity);
                petInstance.GetComponent<NetworkObject>().Spawn();
                petInstance.GetComponent<FollowStimulus>().SetTargetRbody(rigidbody2D);
            }
        }
    }
}
