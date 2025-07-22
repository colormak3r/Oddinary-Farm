using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DirectionalShield : NetworkBehaviour, IDamageable
{
    [Header("Properties")]
    [SerializeField]
    private Hostility hostility;
    public Hostility Hostility => hostility;
    [SerializeField]
    private float recoilDistance = 0.25f;
    public uint CurrentHealthValue => 999; // Shield is invincible, this value should not be changed

    private Vector2 startPosition;

    private void Awake()
    {
        startPosition = transform.localPosition;
    }

    public bool TakeDamage(uint damage, DamageType type, Hostility attackerHostility, Transform attacker)
    {
        //if (showDebugs) Debug.Log($"GetDamaged: Damage = {damage}, type = {type}, hostility = {attackerHostility}, from {attacker.gameObject} to {gameObject}", this);

        // Check if the attacker is hostile towards this entity
        // If the attacker is neutral, it will also damage neutral entities
        if (Hostility == attackerHostility && attackerHostility != Hostility.Neutral) return false;

        TakeDamageRpc();

        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void TakeDamageRpc()
    {
        TriggerHitRecoil();
    }

    public void TriggerHitRecoil()
    {
        // Get the direction opposite of the facing direction (based on rotation)
        float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 recoilDir = new Vector2(-Mathf.Cos(angleRad), -Mathf.Sin(angleRad));

        // Apply recoil
        transform.localPosition += (Vector3)(recoilDir * recoilDistance);

        // Start recoil coroutine if not already running
        if (recoilCoroutine != null) StopCoroutine(recoilCoroutine);
        recoilCoroutine = StartCoroutine(RecoilCoroutine());
    }


    private Coroutine recoilCoroutine;
    private IEnumerator RecoilCoroutine()
    {
        Vector3 target = startPosition;
        Vector3 velocity = Vector3.zero;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, target, ref velocity, 0.1f);
            yield return null;
        }

        transform.localPosition = target;
        recoilCoroutine = null;
    }
}
