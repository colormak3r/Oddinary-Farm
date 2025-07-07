using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ChihuahuaRescue : NetworkBehaviour, IInteractable
{
    [Header("Chihuahua Rescue Settings")]
    [SerializeField]
    private float interactionDuration = 5f;
    [SerializeField]
    private GameObject confettiPrefab;
    [SerializeField]
    private MinMaxFloat barkCdr = new MinMaxFloat(0.1f, 2f);
    [SerializeField]
    private AudioClip[] barkingSounds;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;

    public bool IsHoldInteractable => true;
    private bool isCollected = false;

    private Coroutine interactionCoroutine;
    private AudioElement audioElement;

    private void Awake()
    {
        audioElement = GetComponent<AudioElement>();
    }

    public override void OnNetworkSpawn()
    {
        StartCoroutine(BarkCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        Selector.Main.SetCurrentFill(0f);
    }

    private IEnumerator BarkCoroutine()
    {
        while (true)
        {
            audioElement.PlayOneShot(barkingSounds.GetRandomElement(), true);
            yield return new WaitForSeconds(barkCdr.value);
        }
    }

    public void Interact(Transform source)
    {
        throw new System.NotImplementedException();
    }

    public void InteractionStart(Transform source)
    {
        if (PetManager.Main.IsPetCollected(PetType.Chihuahua))
        {
            if (showDebugs) Debug.Log("Chihuahua already collected.");
            AudioManager.Main.PlaySoundEffect(SoundEffect.UIError);
            return;
        }

        if (PetManager.Main.PetSpawned)
        {
            if (showDebugs) Debug.Log("A pet has already spawned.");
            AudioManager.Main.PlaySoundEffect(SoundEffect.UIError);
            return;
        }

        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(InteractionCoroutine(source));
    }

    private IEnumerator InteractionCoroutine(Transform source)
    {
        yield return Selector.Main.AnimateFill(interactionDuration);
        RequestRescueRpc(source.gameObject);
        Selector.Main.SetCurrentFill(0f);
    }

    [Rpc(SendTo.Server)]
    public void RequestRescueRpc(NetworkObjectReference interacterRef)
    {
        if (interacterRef.TryGet(out var interacterNetObj))
        {
            if (interacterNetObj == null) return;
        }

        if (isCollected) return;
        isCollected = true;

        PetManager.Main.SpawnPet(PetType.Chihuahua, interacterNetObj.transform.position, interacterNetObj.gameObject);
        ConfirmRescueRpc(interacterNetObj.OwnerClientId);
        Destroy(gameObject);
    }

    [Rpc(SendTo.Everyone)]
    public void ConfirmRescueRpc(ulong id)
    {
        if (NetworkManager.Singleton.LocalClientId == id) PetManager.Main.CollectPet(PetType.Chihuahua);
        Instantiate(confettiPrefab, transform.position + new Vector3(0, 1, 0), Quaternion.identity);
    }


    public void InteractionEnd(Transform source)
    {
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        Selector.Main.SetCurrentFill(0f);
    }
}
