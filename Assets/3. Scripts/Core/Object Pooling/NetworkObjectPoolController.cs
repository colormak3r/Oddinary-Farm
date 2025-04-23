using System;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class NetworkObjectPoolController : NetworkBehaviour
{
    [SerializeField]
    private string guid;
    public string Guid => guid;

    private NetworkVariable<bool> IsControlledSpawn = new NetworkVariable<bool>(false);

    private Rigidbody2D rbody2D;
    private INetworkObjectPoolBehaviour[] behaviours;
    private ComponentDefaultState<Collider2D>[] colliderDefaultStates;

#if UNITY_EDITOR
    [ContextMenu("Generate GUID")]
    public void GenerateGUID()
    {
        guid = System.Guid.NewGuid().ToString();

        // Record object state for undo functionality and mark as dirty
        Undo.RecordObject(this, "Generate GUID");
        EditorUtility.SetDirty(this);
    }
#endif

    private void Awake()
    {
        if (guid == "") Debug.LogError("GUID is empty.", gameObject);

        rbody2D = GetComponent<Rigidbody2D>();
        behaviours = GetComponents<INetworkObjectPoolBehaviour>();

        var colliders = GetComponentsInChildren<Collider2D>(true);
        colliderDefaultStates = new ComponentDefaultState<Collider2D>[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderDefaultStates[i] = new ComponentDefaultState<Collider2D>(colliders[i].enabled, colliders[i]);
        }
    }

    public override void OnNetworkSpawn()
    {
        IsControlledSpawn.OnValueChanged += OnControlledSpawnChanged;
        OnControlledSpawnChanged(false, IsControlledSpawn.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsControlledSpawn.OnValueChanged -= OnControlledSpawnChanged;
    }

    private void OnControlledSpawnChanged(bool previousValue, bool isControlledSpawned)
    {
        if (isControlledSpawned)
            NetworkSpawnInternal();
        else
            NetworkDespawnInternal();
    }

    public void NetworkSpawn()
    {
        IsControlledSpawn.Value = true;
    }

    private void NetworkSpawnInternal()
    {
        gameObject.SetActive(true);

        /*foreach (var colliderDefaultState in colliderDefaultStates)
        {
            colliderDefaultState.Component.enabled = colliderDefaultState.DefaultState;
        }*/
        if (rbody2D != null)
        {
            rbody2D.simulated = true;
        }

        foreach (var behaviour in behaviours)
        {
            behaviour.NetworkSpawn();
        }
    }

    public void NetworkDespawn()
    {
        IsControlledSpawn.Value = false;
    }

    private void NetworkDespawnInternal()
    {
        foreach (var behaviour in behaviours)
        {
            behaviour.NetworkDespawn();
        }

        /*foreach (var colliderDefaultState in colliderDefaultStates)
        {
            colliderDefaultState.Component.enabled = false;
        }*/

        if (rbody2D != null)
        {
            rbody2D.linearVelocity = Vector2.zero;
            rbody2D.angularVelocity = 0f;
            rbody2D.simulated = false;
        }

        gameObject.SetActive(false);
    }
}