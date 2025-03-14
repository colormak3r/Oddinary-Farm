using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Transform turretHeadTransform;
    [SerializeField]
    private ProjectileGunProperty projectileGunProperty;

    private TargetDetector targetDetector;
    private ProjectileGun projectileGun;

    private float nextFire;

    private void Awake()
    {
        targetDetector = GetComponent<TargetDetector>();
        projectileGun = GetComponent<ProjectileGun>();
    }
    private void OnEnable()
    {
        targetDetector.OnTargetDetected.AddListener(HandleTargetDetected);
    }

    private void OnDisable()
    {
        targetDetector.OnTargetDetected.RemoveListener(HandleTargetDetected);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // Set the property value
            projectileGun.Initialize(projectileGunProperty);
        }
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        if (targetDetector.CurrentTarget)
        {
            // Rotate towards target
            Vector3 direction = targetDetector.CurrentTarget.position - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            turretHeadTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Shoot
            if (Time.time > nextFire)
            {
                nextFire = Time.time + projectileGunProperty.PrimaryCdr;
                if (projectileGun == null)
                {
                    Debug.LogError("ProjectileGun is null", this);
                }
                if (targetDetector == null)
                {
                    Debug.LogError("TargetDetector is null", this);
                }
                if (targetDetector.CurrentTarget == null)
                {
                    Debug.LogError("Target is null", this);
                }
                projectileGun.OnPrimaryAction(targetDetector.CurrentTarget.position);
            }
        }
    }

    private void HandleTargetDetected(Transform target)
    {

    }

    private void HandleTargetEscaped(Transform target)
    {

    }
}
