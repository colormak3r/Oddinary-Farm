using System.Collections;
using UnityEngine;

public class LazerDeerGun : MonoBehaviour
{
    [Header("Lazer Deer Gun Settings")]
    [SerializeField]
    private Transform gunRing;
    [SerializeField]
    private float shootingDuration = 1f; // duration of the shooting animation
    [SerializeField]
    private float shootStartScale = 5f;
    [SerializeField]
    private float shootEndScale = 10f;
    [SerializeField]
    private GameObject riftPrefab;
    [SerializeField]
    private ParticleSystem shootEffect;

    [Header("Lazer Deer Gun Debugs")]
    [SerializeField]
    private Transform currentTarget;
    [SerializeField]
    private bool isShooting = false;

    private LineRenderer laserRenderer;

    private void Awake()
    {
        laserRenderer = GetComponent<LineRenderer>();
        laserRenderer.positionCount = 0; // Clear the laser line
    }

    private void Update()
    {
        if (!isShooting)
        {
            if (currentTarget)
                RotateGun(currentTarget.position);
            else
                RotateGun(gunRing.position);
        }
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void ShootLaser()
    {
        if (shootLaserCoroutine != null) StopCoroutine(shootLaserCoroutine);
        shootLaserCoroutine = StartCoroutine(ShootLaserRoutine(currentTarget));
    }

    private Coroutine shootLaserCoroutine;
    private IEnumerator ShootLaserRoutine(Transform target)
    {
        isShooting = true;

        var targetCurrentPosition = target.position;
        yield return null;
        var targetNextPosition = target.position;
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

        laserRenderer.positionCount = 2;
        float elapsed = 0f;
        while (elapsed < shootingDuration)
        {
            float t = elapsed / shootingDuration;
            Vector2 shootingPos = Vector2.Lerp(startPosition, endPosition, t);

            RotateGun(shootingPos);

            laserRenderer.SetPosition(0, transform.position);
            laserRenderer.SetPosition(1, shootingPos);

            shootEffect.transform.position = shootingPos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        laserRenderer.positionCount = 0; // Clear the laser line
        shootEffect.Stop();

        isShooting = false;
    }

    private void RotateGun(Vector3 position)
    {
        var dir = position - transform.position;
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 180f; // +180 for the –X artwork
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
