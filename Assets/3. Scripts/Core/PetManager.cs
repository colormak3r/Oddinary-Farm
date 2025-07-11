/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/09/2025
 * Last Modified:   07/09/2025 (Khoa)
 * Notes:           <write here>
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PetManager : NetworkBehaviour
{
    public static PetManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // TODO: PetManagerUI
        //DontDestroyOnLoad(gameObject);
        FetchCollectedPet();
    }

    [Header("Pet Manager Settings")]
    [SerializeField]
    private bool canSpawnPet = true;
    [SerializeField]
    private PetData[] petDataEntries;
    public PetData[] PetDataEntries => petDataEntries;
    [SerializeField]
    private GameObject chihuahuaRescuePrefab;

    [Header("Pet Manager Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool petSpawned = false;
    public bool PetSpawned => petSpawned;
    [SerializeField]
    private PetData petToSpawn;

    private Dictionary<PetType, bool> petCollectionStatus = new Dictionary<PetType, bool>();

    private void FetchCollectedPet()
    {
        foreach (var petData in petDataEntries)
        {
            if (PlayerPrefs.GetInt($"{petData.petType.ToString()}Collected", 0) == 1)
            {
                petCollectionStatus[petData.petType] = true;
                if (showDebugs) Debug.Log($"Pet {petData.petType} is collected.");

            }
            else
            {
                petCollectionStatus[petData.petType] = false;
                if (showDebugs) Debug.Log($"Pet {petData.petType} is not collected.");
            }
        }
    }

    public void InitializeOnLocalClient(GameObject playerObject)
    {
        if (petToSpawn)
        {
            SpawnPet(petToSpawn.petType, playerObject.transform.position, playerObject);
        }
        else
        {
            if (!IsPetCollected(PetType.Chihuahua))
            {
                // If chihuahua is not collected, trigger rescue sequence
                if (showDebugs) Debug.Log("Chihuahua not collected, triggering rescue sequence.");
                RequestRescueChihuahuaSequenceRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestRescueChihuahuaSequenceRpc()
    {
        StartCoroutine(WaitGameManager());
    }

    private IEnumerator WaitGameManager()
    {
        yield return new WaitUntil(() => GameManager.Main.IsInitialized);
        var position = WorldGenerator.Main.RandomChihuahuaRescuePosition;
        var rescueObj = Instantiate(chihuahuaRescuePrefab, position, Quaternion.identity);
        rescueObj.GetComponent<NetworkObject>().Spawn();
        //if (showDebugs)
        Debug.Log($"Chihuahua rescue sequence started at {position}.");
    }

    public void SpawnPet(PetType petType, Vector2 position, GameObject owner)
    {
        if (canSpawnPet) SpawnPetRpc(petType, position, owner);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPetRpc(PetType petType, Vector2 position, NetworkObjectReference ownerRef)
    {
        if (ownerRef.TryGet(out NetworkObject ownerNetObj))
        {
            SpawnPetInternal(petType, position, ownerNetObj.gameObject);
        }
    }

    private void SpawnPetInternal(PetType petType, Vector2 position, GameObject owner)
    {
        var rigidbody2D = owner.GetComponent<Rigidbody2D>();
        foreach (var petData in petDataEntries)
        {
            if (petData.petType == petType)
            {
                var petInstance = Instantiate(petData.petPrefab, position, Quaternion.identity);
                petInstance.GetComponent<NetworkObject>().Spawn();
                petInstance.GetComponent<FollowStimulus>().SetTargetRbody(rigidbody2D);
            }
        }

        SetPetSpawnClientRpc(RpcTarget.Single(owner.GetComponent<NetworkObject>().OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SetPetSpawnClientRpc(RpcParams rpcParams)
    {
        petSpawned = true;
        if (showDebugs) Debug.Log($"PetSpawned set on client {rpcParams.Receive.SenderClientId}.");
    }

    public void CollectPet(PetType petType)
    {
        if (petCollectionStatus.ContainsKey(petType) && !petCollectionStatus[petType])
        {
            petCollectionStatus[petType] = true;
            PlayerPrefs.SetInt($"{petType.ToString()}Collected", 1);
            PlayerPrefs.Save();
            if (showDebugs) Debug.Log($"Pet {petType} collected.");
        }
        else
        {
            if (showDebugs) Debug.Log($"Pet {petType} is already collected or not found.");
        }
    }

    public bool IsPetCollected(PetType petType)
    {
        if (!petCollectionStatus.ContainsKey(petType))
        {
            if (showDebugs) Debug.LogWarning($"Pet {petType} not found in collection status.");
            return false;
        }
        else
        {
            return petCollectionStatus[petType];
        }
    }

    [ContextMenu("Reset Collection Data")]
    public void ResetCollectionData()
    {
        foreach (var petData in petDataEntries)
        {
            PlayerPrefs.SetInt($"{petData.petType.ToString()}Collected", 0);
            petCollectionStatus[petData.petType] = false;
            if (showDebugs) Debug.Log($"Pet {petData.petType} collection status reset.");
        }
    }

    public void SetPetToSpawn(PetData petToSpawn)
    {
        this.petToSpawn = petToSpawn;
    }
}
