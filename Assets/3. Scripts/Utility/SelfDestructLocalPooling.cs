using System.Collections;
using UnityEngine;

public class SelfDestructLocalPooling : MonoBehaviour, ILocalObjectPoolBehaviour
{
    [SerializeField]
    private float lifetime = 1f;

    private float nextDespawnTime;

    public void LocalDespawn()
    {

    }

    public void LocalSpawn()
    {
        if (selfDestructCoroutine != null) StopCoroutine(selfDestructCoroutine);
        selfDestructCoroutine = StartCoroutine(SelfDestructCoroutine());
    }

    private Coroutine selfDestructCoroutine;
    private IEnumerator SelfDestructCoroutine()
    {
        yield return new WaitForSeconds(lifetime);
        LocalObjectPooling.Main.Despawn(gameObject, true);
    }
}
