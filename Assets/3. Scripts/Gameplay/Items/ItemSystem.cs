/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/18/2025 (Khoa)
 * Notes:           <write here>
*/

using ColorMak3r.Utility;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSystem : NetworkBehaviour
{
    [Header("Item System Settings")]
    [SerializeField]
    private Vector2 offset = new Vector2(0, 0.5f);
    [SerializeField]
    private Transform muzzleTransform;
    [SerializeField]
    private GameObject muzzleFlash;
    [SerializeField]
    private Transform recoilTransform;
    [SerializeField]
    private Transform graphicTransform;
    [SerializeField]
    private GameObject meleeAnimationObject;
    private Animator meleeAnimationAnimator;

    [Header("Asset Spawn Preset")]
    [SerializeField]
    private bool recordPlantSpawns = false;
    [SerializeField]
    private bool recordFarmPlotSpawns = false;
    [SerializeField]
    private bool recordStructureSpawns = false;
    [SerializeField]
    private AssetSpawnPreset assetSpawnPreset;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs = false;
    [SerializeField]
    private bool showGizmos = false;

    private Vector2 ObjectPosition => (Vector2)transform.position + offset;

    private EntityStatus entityStatus;
    private Animator animator;
    private LassoController lassoController;

    private Vector2 recoilDefaultPosition;

    private void Awake()
    {
        entityStatus = GetComponent<EntityStatus>();
        lassoController = GetComponent<LassoController>();
        animator = GetComponent<Animator>();
        if (meleeAnimationObject)
            meleeAnimationAnimator = meleeAnimationObject.GetComponent<Animator>();
        else
            Debug.LogWarning($"Melee Animation Object is not set, melee animations of {name} will not play.", this);

        if (recoilTransform) recoilDefaultPosition = recoilTransform.localPosition;
    }

    #region Utility
    public bool IsInRange(Vector2 position, float range)
    {
        return ((Vector2)transform.position - position).magnitude < range;
    }

    public Collider2D OverlapArea(Vector2 size, Vector2 position, LayerMask layers, float precision = 0.9f)
    {
        // Calculate the scaled half-size based on the given precision
        Vector2 scaledHalfSize = size * 0.5f * precision;

        // Define the corners of the overlap area
        Vector2 pointA = position + scaledHalfSize;
        Vector2 pointB = position - scaledHalfSize;

        // Perform the area overlap check and return the result
        return Physics2D.OverlapArea(pointA, pointB, layers);
    }

    public Collider2D[] OverlapAreaAll(Vector2 size, Vector2 position, LayerMask layers, float precision = 0.9f)
    {
        // Calculate the scaled half-size based on the given precision
        Vector2 scaledHalfSize = size * 0.5f * precision;

        // Define the corners of the overlap area
        Vector2 pointA = position + scaledHalfSize;
        Vector2 pointB = position - scaledHalfSize;

        // Perform the area overlap check and return the result
        return Physics2D.OverlapAreaAll(pointA, pointB, layers);
    }
    #endregion

    float debug_radius;
    Vector2 debug_meleePosition;
    #region Melee Weapon
    public void DealDamage(Vector2 position, MeleeWeaponProperty meleeWeaponProperty)
    {
        // Play the melee animation on all clients
        var direction = (position - ObjectPosition);
        float distance = direction.magnitude;
        if (distance > meleeWeaponProperty.Range)
            direction = direction.normalized * meleeWeaponProperty.Range;
        else
            direction = direction.normalized * distance;
        var clampedPosition = ObjectPosition + direction;
        debug_meleePosition = clampedPosition;
        debug_radius = meleeWeaponProperty.Radius;
        PlayMeleeAnimationRpc(position, meleeWeaponProperty.Range, meleeWeaponProperty.Radius);

        // Check if the melee weapon has a valid radius and range
        var hits = Physics2D.CircleCastAll(transform.position, meleeWeaponProperty.Radius, position - (Vector2)transform.position, meleeWeaponProperty.Range, meleeWeaponProperty.DamageableLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                var collider = hit.collider;
                if (collider.gameObject == gameObject) continue;

                // Deal damage to the object
                if (collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(meleeWeaponProperty.Damage, meleeWeaponProperty.DamageType, meleeWeaponProperty.Hostility, transform.root);

                    if (damageable.Hostility == meleeWeaponProperty.Hostility)
                    {
                        if (meleeWeaponProperty.DamageType == DamageType.Slash || meleeWeaponProperty.CanHarvest)
                        {
                            // Check if the object is a plant
                            if (collider.TryGetComponent<Plant>(out var plant))
                            {
                                plant.GetHarvested(transform.root);
                            }
                        }
                    }
                    else
                    {
                        // Check if the object is already dead
                        if (damageable.CurrentHealthValue == 0) continue;

                        if (collider.TryGetComponent<EntityMovement>(out var movement))
                        {
                            movement.Knockback(meleeWeaponProperty.KnockbackForce, transform.position);
                        }
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void PlayMeleeAnimationRpc(Vector2 position, float maxRange, float radius)
    {
        if (meleeAnimationObject != null && meleeAnimationAnimator != null)
        {
            var direction = (position - ObjectPosition);

            // Clamp the direction to the max range
            float distance = direction.magnitude;
            if (distance > maxRange)
                direction = direction.normalized * maxRange;
            else
                direction = direction.normalized * distance;
            var clampedPosition = ObjectPosition + direction;

            // Set the angle of the melee animation object
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var org = angle;
            if (graphicTransform.localScale.x < 0) angle -= 180f;

            meleeAnimationObject.transform.localScale = new Vector2(radius, radius);
            meleeAnimationObject.transform.rotation = Quaternion.Euler(0, 0, angle);
            meleeAnimationObject.transform.position = clampedPosition;
            meleeAnimationAnimator.SetTrigger("Play");
        }
    }

    #endregion

    #region Ranged Weapon
    // This method can run on both server and client
    public virtual void ShootProjectiles(Vector2 position, RangedWeaponProperty rangedWeaponProperty)
    {
        SpawnProjectile(position, rangedWeaponProperty, transform, true);
        ShootProjectilesRpc(position, rangedWeaponProperty, gameObject);
    }

    [Rpc(SendTo.NotMe)]
    private void ShootProjectilesRpc(Vector3 position, RangedWeaponProperty rangedWeaponProperty, NetworkObjectReference ownerRef)
    {
        Transform owner = null;
        if (ownerRef.TryGet(out var networkObject))
        {
            owner = networkObject.transform;
        }
        else
        {
            Debug.LogError("Failed to get owner transform");
            return;
        }

        SpawnProjectile(position, rangedWeaponProperty, owner, false);
    }

    private void SpawnProjectile(Vector3 position, RangedWeaponProperty rangedWeaponProperty, Transform owner, bool isAuthoritative)
    {
        // Todo: Create isInitialized bool and check it here instead
        if (!muzzleTransform) return;

        if (muzzleFlash != null) StartCoroutine(MuzzleFlashCoroutine());

        float spread = 0f;
        float step = 0f;

        if (rangedWeaponProperty.SpreadEvenly && rangedWeaponProperty.ProjectileCount > 1)
        {
            step = rangedWeaponProperty.ProjectileSpread / (rangedWeaponProperty.ProjectileCount - 1);
            spread = -rangedWeaponProperty.ProjectileSpread / 2;
        }

        for (int i = 0; i < rangedWeaponProperty.ProjectileCount; i++)
        {
            float currentSpread;

            if (!rangedWeaponProperty.SpreadEvenly)
            {
                currentSpread = Random.Range(-rangedWeaponProperty.ProjectileSpread, rangedWeaponProperty.ProjectileSpread);
            }
            else
            {
                currentSpread = spread;
                spread += step;
            }

            var muzzlePosition = muzzleTransform.position;
            var direction = Quaternion.Euler(0, 0, currentSpread) * (position - muzzlePosition).normalized;
            var rotation = Quaternion.LookRotation(Vector3.forward, Quaternion.Euler(0, 0, 90) * direction);
            var projectile = LocalObjectPooling.Main.Spawn(AssetManager.Main.ProjectilePrefab);
            projectile.transform.position = muzzlePosition;
            projectile.transform.rotation = rotation;
            projectile.GetComponent<Projectile>().Initialize(owner, rangedWeaponProperty.ProjectileProperty, isAuthoritative);
        }
    }


    private IEnumerator MuzzleFlashCoroutine()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        muzzleFlash.SetActive(false);
    }

    #endregion

    #region Clear Foliage
    public void ClearFoliage(Vector2 position)
    {
        ClearFoliageRpc(position);
    }

    [Rpc(SendTo.Everyone)]
    private void ClearFoliageRpc(Vector2 position)
    {
        position = position.SnapToGrid();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 offset = new Vector2(x, y);
                Vector2 gridPos = (position + offset).SnapToGrid();

                WorldGenerator.Main.RemoveFoliageOnClient(gridPos);
            }
        }
    }
    #endregion

    #region Farm Plot
    public void SpawnFarmPlot(Vector2 position)
    {
        SpawnFarmPlotRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void SpawnFarmPlotRpc(Vector2 position)
    {
        GameObject go = Instantiate(AssetManager.Main.FarmPlotPrefab, position - TransformUtility.HALF_UNIT_Y_V2, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();

        if (recordFarmPlotSpawns)
        {
            RecordPrefabSpawn(AssetManager.Main.FarmPlotPrefab, position);
        }
    }

    public void RemoveFarmPlot(Vector2 position)
    {
        RemoveFarmPlotRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void RemoveFarmPlotRpc(Vector2 position)
    {
        var farmPlotCollider = Physics2D.OverlapPoint(position, LayerManager.Main.FarmPlotLayer);
        if (farmPlotCollider)
        {
            if (recordFarmPlotSpawns)
            {
                RemovePrefabSpawn(farmPlotCollider.transform.root.gameObject, farmPlotCollider.transform.position);
            }

            farmPlotCollider.GetComponent<NetworkObject>().Despawn();
        }
    }

    public void RemovePlant(Vector2 position, GameObject prefer = null)
    {
        RemovePlantRpc(position, prefer);
    }

    [Rpc(SendTo.Server)]
    private void RemovePlantRpc(Vector2 position, NetworkObjectReference preferRef)
    {
        var plantCollider = Physics2D.OverlapPoint(position, LayerManager.Main.PlantLayer);
        if (plantCollider && plantCollider.TryGetComponent(out Plant plant))
        {
            if (recordPlantSpawns)
            {
                RemoveSpawnerSpawn(plantCollider.transform.position);
            }

            AssetManager.Main.SpawnItem(plant.Seed, position, preferRef);
            plant.GetComponent<NetworkObject>().Despawn();
        }
    }
    #endregion

    #region Structure
    public void FixStructure(Vector2 position, LayerMask structureLayer)
    {
        var structureHit = Physics2D.OverlapPoint(position, structureLayer);
        if (structureHit && structureHit.TryGetComponent(out StructureStatus structureStatus))
        {
            structureStatus.GetHealed(1);
        }
    }

    public void RemoveStructure(Vector2 position, LayerMask structureLayer)
    {
        var structureHit = Physics2D.OverlapPoint(position, structureLayer);
        if (structureHit && structureHit.TryGetComponent(out Structure structure))
        {
            if (recordStructureSpawns)
            {
                RemoveSpawnerSpawn(structureHit.transform.position);
            }

            if (structureHit.TryGetComponent(out EntityStatus entityStatus))
            {
                if (entityStatus.CurrentHealthValue == entityStatus.MaxHealth)
                {
                    structure.RemoveStructure();
                }
                else
                {
                    entityStatus.HealthBarUI.FlashAutoHide();
                    AudioManager.Main.PlaySoundEffect(SoundEffect.UIError);
                }

            }
            else
            {
                structure.RemoveStructure();
            }

            Previewer.Main.Show(false);
        }
    }
    #endregion

    #region Spawner

    public void Spawn(Vector2 position, SpawnerProperty spawnerProperty)
    {
        SpawnRpc(position, spawnerProperty);
    }

    [Rpc(SendTo.Server)]
    private void SpawnRpc(Vector2 position, SpawnerProperty spawnerProperty)
    {
        var positionOffset = position + spawnerProperty.SpawnOffset;
        var gameobject = Instantiate(spawnerProperty.PrefabToSpawn, positionOffset, Quaternion.identity);
        gameobject.GetComponent<NetworkObject>().Spawn();
        if (spawnerProperty.InitScript)
            gameobject.GetComponent<IItemInitable>().Initialize(spawnerProperty.InitScript);
        if (spawnerProperty.ClearFoliage)
            ClearFoliageRpc(position);

        if ((recordPlantSpawns && spawnerProperty is SeedProperty)
            || (recordStructureSpawns && spawnerProperty is BlueprintProperty))
        {
            RecordSpawnerSpawn(spawnerProperty, position, spawnerProperty.SpawnOffset);
        }
    }
    #endregion

    #region Net Capture

    public void NetCapture(Vector2 position, float range)
    {
        NetCaptureRpc(position, range);
    }

    [Rpc(SendTo.Server)]
    public void NetCaptureRpc(Vector2 position, float range)
    {
        var direction = position - ObjectPosition;
        var hits = Physics2D.RaycastAll(ObjectPosition, direction, range, LayerManager.Main.AnimalLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent(out CaptureController captureController) && captureController.IsCaptureable)
            {
                captureController.Capture(CaptureType.Net);
            }
        }
    }

    #endregion

    #region Consummable
    public void UseConsummableSelf(ConsummableProperty consummableProperty)
    {
        UseConsummableSelfRpc(consummableProperty);
    }

    [Rpc(SendTo.Server)]
    private void UseConsummableSelfRpc(ConsummableProperty consummableProperty)
    {
        uint healAmount = Convert.ToUInt32(consummableProperty.HealAmount);
        entityStatus.GetHealed(healAmount);
    }
    #endregion

    #region Lasso

    public void LassoPrimary(Vector2 position)
    {
        if (lassoController.IsRecovering) return;

        if (lassoController.CurrentStateValue == LassoState.Capturing)
        {
            lassoController.LassoPull();
        }
        else
        {
            lassoController.ThrowLasso(position);
        }
    }

    public void LassoSecondary()
    {
        lassoController.CancelLasso();
    }

    #endregion

    #region Laser Gun

    public void ShootLaser(Vector2 position, LaserWeaponProperty property, float duration)
    {
        ShootLaserRpc(muzzleTransform.position, position, property, duration);
    }

    [Rpc(SendTo.Everyone)]
    private void ShootLaserRpc(Vector2 startPosition, Vector2 targetPosition, LaserWeaponProperty property, float duration)
    {
        var laserBeam = LocalObjectPooling.Main.Spawn(AssetManager.Main.LaserBeamPrefab);

        // Set the width scale with duration
        var width = Mathf.Min(property.Width, Mathf.Max(0.25f, (duration / property.MaxDuration) * property.Width));

        laserBeam.GetComponent<LaserBeam>().SetLaserBeam(startPosition, targetPosition, property.Range, width, duration);

        if (IsOwner)
        {
            StartCoroutine(ShootLaserCoroutine(startPosition, targetPosition, property, duration));
        }
    }

    private IEnumerator ShootLaserCoroutine(Vector2 startPosition, Vector2 targetPosition, LaserWeaponProperty property, float duration)
    {
        var startTime = Time.time;
        var endTime = startTime + duration;
        var calculatedDamage = (uint)Mathf.Max(1, property.Damage * (uint)Mathf.FloorToInt(duration / 2 / property.Frequency));
        if (showDebugs) Debug.Log($"Calculated Damage: {calculatedDamage} for duration: {duration} with frequency: {property.Frequency}");

        while (Time.time < endTime)
        {
            var raycasts = Physics2D.CircleCastAll(startPosition, property.Radius, (targetPosition - startPosition).normalized, property.Range, LayerManager.Main.DamageableLayer);
            foreach (var hit in raycasts)
            {
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(calculatedDamage, DamageType.Laser, property.Hostility, transform.root);
                }
            }

            yield return new WaitForSeconds(property.Frequency);
        }
    }

    #endregion

    #region Shovel/Digging

    public void Dig(Vector2 position)
    {
        DigRpc(position);
    }

    [Rpc(SendTo.Server)]
    private void DigRpc(Vector2 position)
    {
        var diggableCollider = Physics2D.OverlapPoint(position, LayerManager.Main.DiggableLayer);
        if (diggableCollider && diggableCollider.TryGetComponent<IDiggable>(out var diggable))
        {
            diggable.Dig(transform);
            if (showDebugs) Debug.Log($"Digging at {position} with {diggableCollider.gameObject.name}");
        }
        else
        {
            if (showDebugs) Debug.LogWarning($"No diggable object found at {position}");
        }
    }

    #endregion

    #region Asset Spawn Preset

    private void RecordPrefabSpawn(GameObject prefab, Vector2 position)
    {
#if UNITY_EDITOR
        if (showDebugs) Debug.Log("Recording spawn: " + prefab.name + " at position: " + position);
        assetSpawnPreset.PrefabPositions.Add(new PrefabPosition(prefab, position));
        EditorUtility.SetDirty(assetSpawnPreset);
        //AssetDatabase.SaveAssets();
#endif
    }

    private void RemovePrefabSpawn(GameObject instance, Vector2 position)
    {
#if UNITY_EDITOR
        var instanceName = instance.name.Replace("(Clone)", "");
        var candidate = -1;
        for (int i = 0; i < assetSpawnPreset.PrefabPositions.Count; i++)
        {
            var prefabPos = assetSpawnPreset.PrefabPositions[i];
            if (prefabPos.Prefab.name == instanceName && Vector2.SqrMagnitude(prefabPos.Position - position) < Mathf.Epsilon)
            {
                candidate = i;
                break;
            }
        }
        if (candidate == -1)
        {
            Debug.LogWarning($"Prefab {instanceName} not found in AssetSpawnPreset");
            return;
        }

        if (showDebugs) Debug.Log("Removing spawn: " + instanceName + " at position: " + position);
        assetSpawnPreset.PrefabPositions.RemoveAt(candidate);
        EditorUtility.SetDirty(assetSpawnPreset);
#endif
    }

    private void RecordSpawnerSpawn(SpawnerProperty spawnerProperty, Vector2 position, Vector2 offset)
    {
#if UNITY_EDITOR
        if (showDebugs) Debug.Log("Recording spawn: " + spawnerProperty.name + " at position: " + position);
        assetSpawnPreset.SpawnerPositions.Add(new SpawnerPosition(spawnerProperty, position, offset));
        EditorUtility.SetDirty(assetSpawnPreset);
        //AssetDatabase.SaveAssets();
#endif
    }

    private void RemoveSpawnerSpawn(Vector2 position)
    {
#if UNITY_EDITOR
        var candidate = -1;
        for (int i = 0; i < assetSpawnPreset.SpawnerPositions.Count; i++)
        {
            var spawnerPos = assetSpawnPreset.SpawnerPositions[i];
            if (Vector2.SqrMagnitude(spawnerPos.Position - (position - spawnerPos.Offset)) < Mathf.Epsilon)
            {
                candidate = i;
                break;
            }
        }
        if (candidate == -1)
        {
            Debug.LogWarning($"Position {position} not found in AssetSpawnPreset");
            return;
        }

        if (showDebugs) Debug.Log($"Removing candidate {candidate} at position: {(position - assetSpawnPreset.SpawnerPositions[candidate].Offset)}");
        assetSpawnPreset.SpawnerPositions.RemoveAt(candidate);
        EditorUtility.SetDirty(assetSpawnPreset);
#endif
    }

    #endregion

    #region Animation

    public void SetTrigger(string animation)
    {
        SetTriggerRpc(new FixedString32Bytes(animation));
    }

    private Coroutine chargeCoroutine;

    [Rpc(SendTo.Everyone)]
    private void SetTriggerRpc(FixedString32Bytes animation)
    {
        var animationStr = animation.ToString();

        if (recoilTransform)
        {
            if (animationStr == "Charge Start")
            {
                chargeCoroutine = StartCoroutine(ChargeCoroutine());
            }
            else if (animationStr == "Charge End")
            {
                if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
                recoilTransform.localPosition = recoilDefaultPosition;
            }
        }
        else
        {
            animator.SetTrigger(animationStr);
        }
    }

    private IEnumerator ChargeCoroutine()
    {
        float duration = 3f; // 3 second ramp-up time
        float elapsed = 0f;

        while (true)
        {
            // Calculate the ramp-up multiplier (0 to 1 over 1 second)
            float rampUp = Mathf.Clamp01(elapsed / duration);

            // Apply the multiplier to the random offset
            float offsetX = Random.Range(-0.125f, 0.125f) * rampUp;
            float offsetY = Random.Range(-0.125f, 0.125f) * rampUp;

            recoilTransform.localPosition = recoilDefaultPosition + new Vector2(offsetX, offsetY);

            // Advance time
            elapsed += Time.deltaTime;

            yield return null;
        }
    }

    #endregion

    public void SetMuzzleOffset(Vector2 offset)
    {
        if (muzzleTransform) muzzleTransform.localPosition = offset;
        if (muzzleFlash) muzzleFlash.transform.localPosition = offset + new Vector2(.3125f, 0);
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            if (debug_meleePosition != Vector2.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(debug_meleePosition, debug_radius);
            }
        }
    }
}
