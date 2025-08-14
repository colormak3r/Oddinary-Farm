using ColorMak3r.Utility;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class LazerDeerGun : NetworkBehaviour
{
    [Header("Lazer Deer Gun Settings")]
    [SerializeField]
    private Transform gunTransform;
    [SerializeField]
    private Transform gunRing;
    [SerializeField]
    private GameObject riftPrefab;
    [SerializeField]
    private ParticleSystem shootEffect;

    [Header("Gun Audio Settings")]
    [SerializeField]
    private AudioClip[] shootSounds;
    [SerializeField]
    private AudioElement audioElement;

    [Header("Lazer Deer Gun Settings")]
    [SerializeField]
    private float shootingDuration = 1f; // duration of the shooting animation
    [SerializeField]
    private float shootStartScale = 5f;
    [SerializeField]
    private float shootEndScale = 10f;
    [SerializeField]
    private Vector3 shootingOffset = new Vector3(0f, 0.5f, 0f); // offset for the shooting position
    [SerializeField]
    private float laserWidth = 0.1f; // width of the laser line
    [SerializeField]
    private uint laserDamage = 1;
    [SerializeField]
    private float laserTickCdr = 0.1f;

    [Header("Lazer Deer Gun Debugs")]
    [SerializeField]
    private NetworkVariable<NetworkObjectReference> CurrentTargetRef = new NetworkVariable<NetworkObjectReference>();
    [SerializeField]
    private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;
    [SerializeField]
    private bool isAuthoritative;
    [SerializeField]
    private bool isShooting = false;
    [SerializeField]
    private bool isDestroyed = false;

    private LaserDeer laserDeer;
    private LineRenderer laserRenderer;

    private void Awake()
    {
        laserDeer = GetComponent<LaserDeer>();
        laserRenderer = gunTransform.GetComponent<LineRenderer>();
        laserRenderer.positionCount = 0; // Clear the laser line
    }

    public override void OnNetworkSpawn()
    {
        CurrentTargetRef.OnValueChanged += OnCurrentTargetChanged;
    }

    public override void OnNetworkDespawn()
    {
        CurrentTargetRef.OnValueChanged -= OnCurrentTargetChanged;
    }

    private void OnCurrentTargetChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
    {
        if (newValue.TryGet(out var targetObject))
        {
            currentTarget = targetObject.transform;
            isAuthoritative = targetObject.OwnerClientId == NetworkManager.Singleton.LocalClientId;
            if (isAuthoritative) Debug.Log($"LazerDeerGun: Target set to {currentTarget.name} for {gameObject.name} (Authoritative: {isAuthoritative})", gameObject);
        }
        else
        {
            currentTarget = null;
            isAuthoritative = false;
        }
    }

    private void Update()
    {
        if (!isShooting && !isDestroyed)
        {
            if (currentTarget)
                RotateGunToward(currentTarget.position + shootingOffset);
            else
                RotateGunToward(gunRing.position);
        }
    }

    public void SetTargetOnServer(Transform target)
    {
        CurrentTargetRef.Value = target ? target.gameObject : null;
        if (target) target.GetComponent<EntityStatus>().OnDeathOnServer.AddListener(() => SetTargetOnServer(null)); // Remove target when it dies
    }

    public void ShootLaserOnServer()
    {
        if (currentTarget == null) return;
        ShootLaserRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShootLaserRpc()
    {
        if (currentTarget == null) return;
        if (shootLaserCoroutine != null) StopCoroutine(shootLaserCoroutine);
        shootLaserCoroutine = StartCoroutine(ShootLaserRoutine(currentTarget));
    }

    private Coroutine shootLaserCoroutine;
    private AudioClip previous;
    private IEnumerator ShootLaserRoutine(Transform target)
    {
        isShooting = true;

        var targetCurrentPosition = target.position + shootingOffset;
        yield return null;
        var targetNextPosition = target.position + shootingOffset;
        if (targetNextPosition == targetCurrentPosition)
            targetNextPosition = targetCurrentPosition + (Vector3)Random.insideUnitCircle * 0.1f; // Ensure there's a slight movement

        var dir = targetNextPosition - targetCurrentPosition;

        var startPosition = targetCurrentPosition - dir.normalized * shootStartScale;
        var endPosition = targetCurrentPosition + dir.normalized * shootEndScale;

        // Rift effect
        var riftObject = LocalObjectPooling.Main.Spawn(riftPrefab);
        var rift = riftObject.GetComponent<Rift>();
        rift.StartRift(startPosition, endPosition, shootingDuration);

        // Shoot effect
        shootEffect.transform.position = startPosition;
        shootEffect.Play();

        var audioclip = shootSounds.GetRandomElementNot(previous);
        audioElement.PlayOneShot(audioclip, false);
        previous = audioclip;

        laserRenderer.positionCount = 2;
        float elapsed = 0f;
        float nextTickTime = 0f;
        while (elapsed < shootingDuration)
        {
            float t = elapsed / shootingDuration;
            Vector2 shootingPos = Vector2.Lerp(startPosition, endPosition, t);

            RotateGunToward(shootingPos);

            laserRenderer.SetPosition(0, gunTransform.position);
            laserRenderer.SetPosition(1, shootingPos);

            shootEffect.transform.position = shootingPos;


            if (isAuthoritative && Time.time > nextTickTime)
            {
                nextTickTime = Time.time + laserTickCdr;
                var hits = Physics2D.OverlapCircleAll(shootingPos, laserWidth, LayerManager.Main.DamageableLayer);
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<IDamageable>(out var damageable))
                    {
                        damageable.TakeDamage(laserDamage, DamageType.Laser, Hostility.Hostile, laserDeer.transform);
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        laserRenderer.positionCount = 0; // Clear the laser line
        shootEffect.Stop();

        isShooting = false;
    }

    private void RotateGunToward(Vector3 position)
    {
        var dir = position - gunTransform.position;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 180f; // +180 for the –X artwork
        gunTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetDestroy()
    {
        SetDestroyRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void SetDestroyRpc()
    {
        isDestroyed = true;
    }
}
