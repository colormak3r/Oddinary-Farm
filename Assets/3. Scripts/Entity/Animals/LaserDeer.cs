using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class LaserDeer : MonoBehaviour
{
    [Header("Laser Deer Settings")]
    [SerializeField]
    private Transform target;
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
    [SerializeField]
    private LazerDeerGun[] laserDeerGuns;

    [Header("Laser Deer SFX")]
    [SerializeField]
    private AudioClip stepSfx;

    private bool toggleStage = false;
    private float nextMoveTime = 0f;
    private Coroutine moveRoutine;
    private float stopDistanceSqr;
    private int currentGunIndex;
    private float nextShootTime = 0f;
    private float shootRangeSqr;

    private CinemachineImpulseSource impulse;
    private AudioElement audioElement;

    private void Awake()
    {
        shootRangeSqr = shootRange * shootRange;
        stopDistanceSqr = stopDistance * stopDistance;
        impulse = GetComponent<CinemachineImpulseSource>();
        audioElement = GetComponent<AudioElement>();
    }

    private void Update()
    {
        if (rotateGunRing) gunRing.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (Time.time > nextShootTime && target && (target.transform.position - transform.position).sqrMagnitude < shootRangeSqr && canShoot)
        {
            nextShootTime = Time.time + shootCdr;
            laserDeerGuns[currentGunIndex].SetTarget(target);
            laserDeerGuns[currentGunIndex].ShootLaser();
            currentGunIndex = (currentGunIndex + 1) % laserDeerGuns.Length; // Cycle through guns
        }

        if (Time.time > nextMoveTime)
        {
            if (target && (target.transform.position - transform.position).sqrMagnitude >= stopDistanceSqr)
            {
                nextMoveTime = Time.time + animationDuration + animationCooldown;
                toggleStage = !toggleStage;
                animator.SetBool("Reverse", target.transform.position.x - transform.position.x > 0f);
                animator.SetBool("ToggleStage", toggleStage);

                // Can move up to moveDistance units towards the target over animationDuration
                if (moveRoutine != null) StopCoroutine(moveRoutine);
                moveRoutine = StartCoroutine(MoveTowardTarget(target));
            }
        }
    }

    private IEnumerator MoveTowardTarget(Transform target)
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
        impulse.GenerateImpulse(impulseStrength);
        audioElement.PlayOneShot(stepSfx);
        moveRoutine = null;
    }
}
