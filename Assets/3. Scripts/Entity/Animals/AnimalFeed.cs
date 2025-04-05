using System;
using Unity.Netcode;
using UnityEngine;

public class AnimalFeed : NetworkBehaviour
{
    private static readonly int DENSITY_ID = Shader.PropertyToID("_Density");

    NetworkVariable<int> FeedTime = new NetworkVariable<int>(3);

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        FeedTime.OnValueChanged += HandleOnFeedTimeChange;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        FeedTime.OnValueChanged -= HandleOnFeedTimeChange;
    }

    private void HandleOnFeedTimeChange(int previousValue, int newValue)
    {
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(DENSITY_ID, newValue);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    public void FeedOnServer()
    {
        if (!IsServer) return;

        if (FeedTime.Value > 0)
        {
            FeedTime.Value--;

            if (FeedTime.Value == 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
