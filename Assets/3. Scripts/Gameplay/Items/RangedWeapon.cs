using Unity.Netcode;
using UnityEngine;

public class RangedWeapon : Item
{
    private RangedWeaponProperty rangedWeaponProperty;
    private Transform muzzleTransform;

    protected override void Initialize()
    {
        base.Initialize();

        muzzleTransform = transform.root.GetComponent<TransformReference>()?.Get();
        if (muzzleTransform == null) Debug.LogError("Failed to get muzzle transform");
    }

    protected override void HandleOnPropertyChanged(ItemProperty previousValue, ItemProperty newValue)
    {
        base.HandleOnPropertyChanged(previousValue, newValue);
        rangedWeaponProperty = (RangedWeaponProperty)newValue;
    }

    // This method can run on both server and client
    protected virtual void ShootProjectiles(Vector2 position)
    {
        SpawnProjectile(position, transform.root, true);
        ShootProjectilesRpc(position, transform.root.gameObject);
    }

    [Rpc(SendTo.NotMe)]
    private void ShootProjectilesRpc(Vector3 lookPosition, NetworkObjectReference ownerRef)
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

        SpawnProjectile(lookPosition, owner, false);
    }

    private void SpawnProjectile(Vector3 lookPosition, Transform owner, bool isAuthoritative)
    {
        for (int i = 0; i < rangedWeaponProperty.ProjectileCount; i++)
        {
            var spread = rangedWeaponProperty.ProjectileSpread;
            if (!rangedWeaponProperty.IsDeterninedSpread)
            {
                spread = Random.Range(-spread, spread);
            }

            var muzzlePosition = muzzleTransform.position;
            var direction = Quaternion.Euler(0, 0, spread) * (lookPosition - muzzlePosition).normalized;
            var rotation = Quaternion.LookRotation(Vector3.forward, Quaternion.Euler(0, 0, 90) * direction);
            var projectile = LocalObjectPooling.Main.Spawn(AssetManager.Main.ProjectilePrefab);
            projectile.transform.position = muzzlePosition;
            projectile.transform.rotation = rotation;
            projectile.GetComponent<Projectile>().Initialize(owner, rangedWeaponProperty.ProjectileProperty, isAuthoritative);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || rangedWeaponProperty == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(PlayerController.MuzzleTransform.position, PlayerController.LookPosition);
    }
}