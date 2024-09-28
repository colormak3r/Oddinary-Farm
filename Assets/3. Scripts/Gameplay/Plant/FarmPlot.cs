using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Properties;
using ColorMak3r.Utility;

public class FarmPlot : NetworkBehaviour, IWaterable
{
    [SerializeField]
    private NetworkVariable<bool> isWatered;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void GetWatered(float duration)
    {
        GetWateredRpc(duration);
    }

    [Rpc(SendTo.Server)]
    private void GetWateredRpc(float duration)
    {
        if (isWatered.Value) StopAllCoroutines();

        isWatered.Value = true;

        StartCoroutine(WateredCoroutine(duration));
    }

    private IEnumerator WateredCoroutine(float duration)
    {
        spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f);
        yield return new WaitForSeconds(duration);

        spriteRenderer.color = new Color(1f, 1f, 1f);

        isWatered.Value = false;
    }
}
