using ColorMak3r.Utility;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class LaserDeer : NetworkBehaviour
{
    [Header("Laser Deer Settings")]
    [SerializeField]
    private bool canTarget = true;
    [SerializeField]
    private LayerMask playerLayerMask;
    [SerializeField]
    private float detectRange = 20f;
    [SerializeField]
    private float escapeRange = 30f;
    [SerializeField]
    private float stopDistance = 5f;

    [Header("Laser Deer Impulse")]
    [SerializeField]
    private float impulseStrength = 0.25f;

    [Header("Laser Deer Animation")]
    [SerializeField]
    private float animationDuration = 0.5f;
    [SerializeField]
    private float animationCooldown = 0.5f;
    [SerializeField]
    private float moveDistance = 0.855f;
    [SerializeField]
    private Animator animator;

    [Header("Laser Deer Gun")]
    [SerializeField]
    private bool canShoot = true; // whether the laser deer can shoot
    [SerializeField]
    private bool rotateGunRing = true;
    [SerializeField]
    private float rotationSpeed = 90f;
    [SerializeField]
    private float shootRange = 15f;
    [SerializeField]
    private float shootCdr = 0.5f; // cooldown for the gun ring rotation
    [SerializeField]
    private Transform gunRing;

    [Header("Laser Deer Death")]
    [SerializeField]
    private float explosionDuration = 10f;
    [SerializeField]
    private float explosionCdr = 0.5f; // cooldown for the explosion effect
    [SerializeField]
    private float explosionRadius = 5f; // radius of the explosion effect
    [SerializeField]
    private GameObject explosionPrefab;

    [Header("Laser Deer SFX")]
    [SerializeField]
    private AudioClip stepSfx;
    [SerializeField]
    private AudioClip[] explosionSfxs;

    [Header("Laser Deer Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool isDestroyed = false; // whether the laser deer is destroyed
    [SerializeField]
    private int componentDestroyed;
    [SerializeField]
    private List<Transform> targetList = new List<Transform>();

    private bool toggleStage = false;
    private float nextMoveTime = 0f;
    private Coroutine moveRoutine;
    private float stopDistanceSqr;
    private int currentGunIndex;
    private float nextShootTime = 0f;
    private float shootRangeSqr;
    private float nextTargetCheckTime = 0f;

    private CinemachineImpulseSource impulse;
    private AudioElement audioElement;
    private LazerDeerGun[] laserDeerGuns;
    private ComponentStatus[] componentStatuses;

    private void Awake()
    {
        shootRangeSqr = shootRange * shootRange;
        stopDistanceSqr = stopDistance * stopDistance;
        impulse = GetComponent<CinemachineImpulseSource>();
        audioElement = GetComponent<AudioElement>();
        componentStatuses = GetComponents<ComponentStatus>();
        laserDeerGuns = GetComponents<LazerDeerGun>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var componentStatus in componentStatuses)
            {
                componentStatus.OnDeathOnServer.AddListener(OnDeathOnServer);
            }
        }
    }

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            Debug.Log("Laser Deer spawned on server, pausing time for the player.");
            TimeManager.Main.PauseTimeOnServer(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            foreach (var componentStatus in componentStatuses)
            {
                componentStatus.OnDeathOnServer.RemoveListener(OnDeathOnServer);
            }
        }
    }

    private void OnDeathOnServer()
    {
        componentDestroyed++;
        if (componentDestroyed >= componentStatuses.Length)
        {
            if (showDebugs) Debug.Log("Laser Deer destroyed, spawning explosion effect.");
            isDestroyed = true;
            targetList.Clear();
            foreach (var gun in laserDeerGuns)
            {
                gun.SetDestroy();
            }
            PlayDeathCoroutineRpc();

            TimeManager.Main.PauseTimeOnServer(false);
        }
    }

    [ContextMenu("Play Death Coroutine Rpc")]
    [Rpc(SendTo.Everyone)]
    private void PlayDeathCoroutineRpc()
    {
        StartCoroutine(DeathCoroutine());
    }

    private IEnumerator DeathCoroutine()
    {
        isDestroyed = true;

        float nextExplosionTime = 0.0f;
        float elapsed = 0f;
        while (elapsed < explosionDuration)
        {
            if (elapsed > nextExplosionTime)
            {
                nextExplosionTime = elapsed + explosionCdr;
                for (int i = 0; i < 3; i++)
                {
                    GameObject explosion = LocalObjectPooling.Main.Spawn(explosionPrefab);
                    explosion.transform.position = transform.position + new Vector3(0, 3) + (Vector3)(UnityEngine.Random.insideUnitCircle * explosionRadius);
                }
                impulse.GenerateImpulse(UnityEngine.Random.insideUnitCircle * impulseStrength);
                audioElement.PlayOneShot(explosionSfxs.GetRandomElement(), false);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    HashSet<Transform> currentHits = new HashSet<Transform>();
    int nextGunGetNewTargetIndex = 0;
    private void Update()
    {
        if (isDestroyed) return;

        if (rotateGunRing) RotateGunRing();

        if (IsServer)
        {
            if (Time.time > nextTargetCheckTime && canTarget)
            {
                nextTargetCheckTime = Time.time + 1f; // Check for targets every 0.5 seconds

                var hits = Physics2D.OverlapCircleAll(transform.position, detectRange, playerLayerMask);
                currentHits.Clear();
                currentHits.UnionWith(hits.Select(hit => hit.transform));

                // Handle new targets
                foreach (var t in currentHits)
                {
                    if (!targetList.Contains(t))
                    {
                        targetList.Add(t);
                        laserDeerGuns[nextGunGetNewTargetIndex].SetTargetOnServer(t); // Set target for the gun
                        nextGunGetNewTargetIndex = (nextGunGetNewTargetIndex + 1) % laserDeerGuns.Length; // Cycle through guns
                    }
                }

                // Handle removed targets
                for (int i = targetList.Count - 1; i >= 0; i--)
                {
                    Transform t = targetList[i];
                    if (!currentHits.Contains(t))
                    {
                        targetList.RemoveAt(i);
                        foreach (var gun in laserDeerGuns)
                        {
                            if (gun.CurrentTarget == t) gun.SetTargetOnServer(null); // Clear target if it was the current target
                        }
                    }
                }
            }

            // Select a new target if the gun has no target
            if (targetList.Count > 0)
            {
                for (int i = 0; i < laserDeerGuns.Length; i++)
                {
                    if (laserDeerGuns[i].CurrentTarget == null)
                    {
                        if (i < targetList.Count)
                        {
                            laserDeerGuns[i].SetTargetOnServer(targetList[i]);
                        }
                        else
                        {
                            laserDeerGuns[i].SetTargetOnServer(targetList[UnityEngine.Random.Range(0, targetList.Count - 1)]);
                        }
                    }
                }
            }
            else
            {
                // If no targets, clear all gun targets
                foreach (var gun in laserDeerGuns)
                {
                    gun.SetTargetOnServer(null);
                }
            }

            if (Time.time > nextShootTime && canShoot)
            {
                nextShootTime = Time.time + shootCdr;
                laserDeerGuns[currentGunIndex].ShootLaserOnServer();
                currentGunIndex = (currentGunIndex + 1) % laserDeerGuns.Length; // Cycle through guns
            }

            if (Time.time > nextMoveTime)
            {
                if (targetList.Count > 0 && (targetList[0].transform.position - transform.position).sqrMagnitude >= stopDistanceSqr)
                {
                    MoveToTarget(targetList[0]);
                }
            }
        }
    }

    private void RotateGunRing()
    {
        gunRing.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    #region Movement

    private void MoveToTarget(Transform target)
    {
        nextMoveTime = Time.time + animationDuration + animationCooldown;
        toggleStage = !toggleStage;
        animator.SetBool("Reverse", target.transform.position.x - transform.position.x > 0f);
        animator.SetBool("ToggleStage", toggleStage);

        // Can move up to moveDistance units towards the target over animationDuration
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTowardTargetCoroutine(target));
    }

    private IEnumerator MoveTowardTargetCoroutine(Transform target)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = (target.position - startPos).normalized;
        Vector3 endPos = startPos + direction * moveDistance;

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;          // 0 -> 1
            transform.position = Vector3.Lerp(startPos, endPos, t);

            elapsed += Time.deltaTime;
            yield return null;                              // wait one frame
        }

        transform.position = endPos;                        // land exactly on the spot
        moveRoutine = null;
    }

    public void PlayStepVSFX()
    {
        impulse.GenerateImpulse(impulseStrength);
        audioElement.PlayOneShot(stepSfx);
    }

    #endregion
}
