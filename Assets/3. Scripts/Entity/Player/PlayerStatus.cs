using ColorMak3r.Utility;
using Steamworks;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStatus : EntityStatus
{
    [Header("Player Settings")]
    [SerializeField]
    private Transform respawnPoint;
    [SerializeField]
    private Collider2D playerHitbox;
    [SerializeField]
    private PlayerNameUI playerNameUI;

    private NetworkVariable<FixedString128Bytes> PlayerName = new NetworkVariable<FixedString128Bytes>(default, default, NetworkVariableWritePermission.Owner);

    public string PlayerNameValue => PlayerName.Value.ToString();
    public PlayerNameUI PlayerNameUI => playerNameUI;

    private IControllable[] controllables;

    protected override void Awake()
    {
        base.Awake();
        controllables = GetComponentsInChildren<IControllable>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        gameObject.name = NetworkObject.OwnerClientId == 0 ? "Host" : $"Client {NetworkObject.OwnerClientId}";

        HandlePlayerNameChange(default, PlayerName.Value);
        PlayerName.OnValueChanged += HandlePlayerNameChange;

        if (IsOwner)
        {
            PlayerName.Value = SteamClient.Name;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        PlayerName.OnValueChanged -= HandlePlayerNameChange;
    }

    private void HandlePlayerNameChange(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        playerNameUI.SetPlayerName(newValue.ToString());
    }

    protected override void OnEntityDeathOnServer()
    {
        // Override to prevent player from being destroyed
        playerHitbox.enabled = false;
    }

    protected override void OnEntityRespawnOnClient()
    {
        healthBarUI.SetValue(CurrentHealthValue, MaxHealth);
    }

    protected override IEnumerator DeathOnClientCoroutine()
    {
        Coroutine audioCoroutine = null;
        Coroutine effectCoroutine = null;
        if (audioElement)
        {
            audioElement.PlayOneShot(deathSound);
            audioCoroutine = StartCoroutine(MiscUtility.WaitCoroutine(deathSound.length));
        }

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Disable all physics
        rbody.linearVelocity = Vector2.zero;
        foreach (var collider in colliders) collider.enabled = false;

        if (IsOwner)
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

        yield return effectCoroutine;
        yield return audioCoroutine;

        // Disable all renderers
        //foreach (var renderer in renderers) renderer.enabled = false;
        foreach (var light in lights) light.enabled = false;

        // Determain respawn position
        var respawnPos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        var deathPos = transform.position;

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
        yield return new WaitUntil(() => transform.position == respawnPos);

        foreach (var collider in colliders) collider.enabled = true;
        //foreach (var renderer in renderers) renderer.enabled = true;
        foreach (var light in lights) light.enabled = true;

        if (IsOwner)
        {
            // Transform pop in
            yield return transform.PopCoroutine(0, 1, 0.5f);

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
