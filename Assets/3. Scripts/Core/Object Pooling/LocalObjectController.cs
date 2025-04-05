using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using ColorMak3r.Utility;

public class ComponentDefaultState<T1, T2>
    where T1 : Component
{
    public bool DefaultState { get; private set; }
    public T1 Component { get; private set; }
    public T2 Metadata { get; private set; }

    public ComponentDefaultState(bool defaultState, T1 component, T2 metadata)
    {
        DefaultState = defaultState;
        Component = component;
        Metadata = metadata;
    }
}

// Overload when no T2 is needed — use ValueTuple as default
public class ComponentDefaultState<T1> : ComponentDefaultState<T1, ValueTuple>
    where T1 : Component
{
    public ComponentDefaultState(bool defaultState, T1 component)
        : base(defaultState, component, default)
    {
    }
}

public class LocalObjectController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private string guid;
    public string Guid => guid;
    private Transform pool;
    public Transform Pool => pool;

    private ILocalObjectPoolingBehaviour[] behaviours;

    private ComponentDefaultState<Collider2D>[] colliderDefaultStates;
    private ComponentDefaultState<SpriteRenderer, Color>[] spriteRendererDedaultStates;
    private Rigidbody2D rbody2D;
    private Vector3 defaultScale;

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
        behaviours = GetComponentsInChildren<ILocalObjectPoolingBehaviour>(true);
        if (guid == "") Debug.LogError("GUID is empty.", gameObject);

        var colliders = GetComponentsInChildren<Collider2D>(true);
        colliderDefaultStates = new ComponentDefaultState<Collider2D>[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderDefaultStates[i] = new ComponentDefaultState<Collider2D>(colliders[i].enabled, colliders[i]);
        }

        var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        spriteRendererDedaultStates = new ComponentDefaultState<SpriteRenderer, Color>[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRendererDedaultStates[i] = new ComponentDefaultState<SpriteRenderer, Color>(spriteRenderers[i].enabled, spriteRenderers[i], spriteRenderers[i].color);
        }

        rbody2D = GetComponentInChildren<Rigidbody2D>(true);
        defaultScale = transform.localScale;
    }

    public void LocalSpawn(Transform pool, bool instant)
    {
        this.pool = pool;

        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        if (instant)
        {
            transform.localScale = defaultScale;

            foreach (var colliderDefaultState in colliderDefaultStates)
            {
                colliderDefaultState.Component.enabled = colliderDefaultState.DefaultState;
            }

            foreach (var localObjectPoolingBehaviour in behaviours)
            {
                localObjectPoolingBehaviour.LocalSpawn();
            }
        }
        else
        {
            StartCoroutine(SpawnCoroutine());
        }
    }

    private IEnumerator SpawnCoroutine()
    {
        yield return transform.PopCoroutine(Vector3.zero, defaultScale);

        foreach (var colliderDefaultState in colliderDefaultStates)
        {
            colliderDefaultState.Component.enabled = colliderDefaultState.DefaultState;
        }

        foreach (var localObjectPoolingBehaviour in behaviours)
        {
            localObjectPoolingBehaviour.LocalSpawn();
        }
    }

    public void LocalDespawn(bool instant)
    {
        if (gameObject.activeInHierarchy == false) return;

        if (instant)
        {
            foreach (var localObjectPoolingBehaviour in behaviours)
            {
                localObjectPoolingBehaviour.LocalDespawn();
            }

            foreach (var colliderDefaultState in colliderDefaultStates)
            {
                colliderDefaultState.Component.enabled = false;
            }

            transform.localScale = Vector3.zero;

            gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(DespawnCoroutine());
        }
    }

    private IEnumerator DespawnCoroutine()
    {
        foreach (var localObjectPoolingBehaviour in behaviours)
        {
            localObjectPoolingBehaviour.LocalDespawn();
        }

        foreach (var colliderDefaultState in colliderDefaultStates)
        {
            colliderDefaultState.Component.enabled = false;
        }

        yield return transform.PopCoroutine(transform.localScale, Vector3.zero);

        gameObject.SetActive(false);
    }
}
