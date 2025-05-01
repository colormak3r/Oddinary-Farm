using ColorMak3r.Utility;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
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

    private Vector2 ObjectPosition => (Vector2)transform.position + offset;

    private EntityStatus entityStatus;
    private void Awake()
    {
        entityStatus = GetComponent<EntityStatus>();
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

    #region Melee Weapon
    public void DealDamage(Vector2 position, MeleeWeaponProperty meleeWeaponProperty)
    {
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

                    if (damageable.GetHostility() == meleeWeaponProperty.Hostility)
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
                        if (damageable.GetCurrentHealth() == 0) continue;

                        if (collider.TryGetComponent<EntityMovement>(out var movement))
                        {
                            movement.Knockback(meleeWeaponProperty.KnockbackForce, transform);
                        }
                    }
                }


            }
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

        for (int i = 0; i < rangedWeaponProperty.ProjectileCount; i++)
        {
            var spread = rangedWeaponProperty.ProjectileSpread;
            if (!rangedWeaponProperty.SpreadEvenly)
            {
                spread = Random.Range(-spread, spread);
            }

            var muzzlePosition = muzzleTransform.position;
            var direction = Quaternion.Euler(0, 0, spread) * (position - muzzlePosition).normalized;
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

                WorldGenerator.Main.InvalidateFolliageOnClient(gridPos);
                WorldGenerator.Main.RemoveFoliage(gridPos);
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
        CreatureSpawnManager.Main.UpdateSafeRadius(Mathf.CeilToInt(Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.y))) + 5);

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

            structure.RemoveStructure();
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
            if (hit.collider.TryGetComponent(out Animal animal) && animal.IsCaptureable)
            {
                animal.Capture();
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
}
