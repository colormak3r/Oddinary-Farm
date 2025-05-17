using ColorMak3r.Utility;       // Custom Library 
using Steamworks;               // Steamworks API

using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    [SerializeField]
    private Transform respawnPoint;
    [SerializeField]
    private Collider2D playerHitbox;        // Capsule collider with isTrigger is hitbox
    [SerializeField]
    private PlayerNameUI playerNameUI;

    // Netowrk variable (value(Empty String), readpermission(Everyone), writepermission (Owner))
    private NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, default, NetworkVariableWritePermission.Owner);

    // NOTE: Consider this if you ever want to write 
    //public string PlayerNameValue
    //{
    //    get => PlayerName.Value.ToString();
    //    set => PlayerName.Value = new FixedString128Bytes(value);
    //}

    public string PlayerNameValue => PlayerName.Value.ToString();
    public PlayerNameUI PlayerNameUI => playerNameUI;       // NOTE: you can use get and set with Serializefield by using the 'field' keyword -> [field: SerializeField] public PlayerNameUI PlayerNameUI { get; private set; };

    private IControllable[] controllables;              // List of objects that are able to be controlled by the player
    private NetworkTransform networkTransform;          

    protected override void Awake()
    {
        base.Awake();
        controllables = GetComponentsInChildren<IControllable>();
        networkTransform = GetComponent<NetworkTransform>();
    }

    // Subscribe to events and init player's name
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Name the game object based on who's the client and the host
        gameObject.name = NetworkObject.OwnerClientId == 0 ? "Host" : $"Client {NetworkObject.OwnerClientId}";

        // Update the player's name in UI
        HandlePlayerNameChange(default, PlayerName.Value);
        PlayerName.OnValueChanged += HandlePlayerNameChange;        // Subscribe to UI event

        if (IsOwner)
        {
            // Get steam name and set to in-game name
            if (SteamClient.IsValid)
                PlayerName.Value = SteamClient.Name;
        }
    }

    // Unsubscribe from events
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerName.OnValueChanged -= HandlePlayerNameChange;
    }

    // NOTE: Possibly rename this method for clarity "UpdatePlayerNameUI"
    private void HandlePlayerNameChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        playerNameUI.SetPlayerName(newValue.ToString());
    }

    protected override void OnEntityDeathOnServer()
    {
        // Override to prevent player from being destroyed
        playerHitbox.enabled = false;
    }

    // Update player health bar UI on respawn
    protected override void OnEntityRespawnOnClient()
    {
        healthBarUI.SetValue(CurrentHealthValue, MaxHealth);
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        Coroutine audioCoroutine = null;
        Coroutine effectCoroutine = null;

        // Play Audio
        if (audioElement)
        {
            audioElement.PlayOneShot(deathSound);
            audioCoroutine = StartCoroutine(MiscUtility.WaitCoroutine(deathSound.length));
        }

        // Play VFX
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Disable all physics
        rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

        if (IsOwner)        // Owner client procedure
        {
            // Detach camera to prevent it being shrink
            Camera.main.transform.parent = null;

            // Disable all controllables
            foreach (var controllable in controllables)
            {
                controllable.SetControllable(false);
            }

            // Transform pop out
            effectCoroutine = StartCoroutine(transform.PopCoroutine(1, 0, 0.25f));
        }

        yield return effectCoroutine;       // Finish Effect
        yield return audioCoroutine;        // Finish Audio

        // Disable all renderers
        //foreach (var renderer in renderers) renderer.enabled = false;
        foreach (var light in lights) light.enabled = false;

        // Determain respawn position
        var respawnPos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        var deathPos = transform.position;

        /*var int_respawnPos = ((Vector2)respawnPos).ToInt();
        if (WorldGenerator.Main.GetElevation(int_respawnPos.x, int_respawnPos.y) < FloodManager.Main.CurrentFloodLevelValue)
        {
            yield break;
        }*/

        // Black out and move player to respawn position
        if (IsOwner)
        {
            yield return new WaitForSeconds(3f);
            yield return TransitionUI.Main.ShowCoroutine();

            transform.position = respawnPos;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);

            yield return TransitionUI.Main.HideCoroutine();
        }

        // Wait until player is at respawn position
        if (IsOwner) networkTransform.Teleport(respawnPos, Quaternion.identity, Vector3.one);
        yield return new WaitUntil(() => Vector3.Distance(transform.position, respawnPos) < 0.01f);

        foreach (var collider in colliders) collider.enabled = true;

        //foreach (var renderer in renderers) renderer.enabled = true;
        foreach (var light in lights) light.enabled = true;

        // Handle Setup and Respawn
        if (IsOwner)
        {
            // Transform pop in
            yield return transform.PopCoroutine(0, 1, 0.5f);

            // Move camera to player
            Camera.main.transform.parent = transform;
            Camera.main.transform.localPosition = new Vector3(0, 0, -10);

            foreach (var controllable in controllables)
            {
                controllable.SetControllable(true);
            }
        }

        Respawn();
    }
}
