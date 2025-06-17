using System.Collections;
using UnityEngine;

public class LaserWeapon : Item
{
    protected LaserWeaponProperty laserWeaponProperty;

    private float currentPitch = 1f;
    private float defaultPitch = 1f;
    private float pitchIncrement = 0.05f;

    private bool isCharging = false;
    public bool IsCharging => isCharging;

    public override void Initialize(ItemProperty baseProperty)
    {
        base.Initialize(baseProperty);
        laserWeaponProperty = (LaserWeaponProperty)baseProperty;
    }

    private float chargeStartTime = -1f;
    private float lastChargeTime = -1f;

    private Coroutine chargeCoroutine;
    public override void OnPrimaryAction(Vector2 position)
    {
        base.OnPrimaryAction(position);

        currentPitch = defaultPitch;

        if (chargeStartTime < 0) chargeStartTime = Time.time;
        lastChargeTime = Time.time;
        isCharging = true;

        if (chargeCoroutine != null) StopCoroutine(chargeCoroutine);
        chargeCoroutine = StartCoroutine(ChargeCoroutine());
        ItemSystem.SetTrigger("Charge Start");
    }

    private IEnumerator ChargeCoroutine()
    {
        var charged = false;
        // Charging up sound effect
        while (Time.time - chargeStartTime < laserWeaponProperty.MaxDuration)
        {
            var chargeTime = Time.time - chargeStartTime;

            if (chargeTime < laserWeaponProperty.Frequency)
                AudioElement.PlayOneShot(laserWeaponProperty.ChargeStartSfx, currentPitch);
            else
                AudioElement.PlayOneShot(laserWeaponProperty.ChargeTickSfx, currentPitch);

            // Play charging sound effect when halfway through the charge duration
            if (chargeTime >= laserWeaponProperty.MaxDuration && !charged)
            {
                charged = true;
                AudioElement.PlayOneShot(laserWeaponProperty.ChargingUpSfx, currentPitch);
            }

            currentPitch += pitchIncrement;
            yield return new WaitForSeconds(laserWeaponProperty.Frequency);
        }

        // Max charge
        while (chargeStartTime > 0)
        {
            AudioElement.PlayOneShot(laserWeaponProperty.ChargeTickSfx, currentPitch);
            yield return new WaitForSeconds(laserWeaponProperty.Frequency / 4);
        }
    }

    public override void OnPrimaryCancel(Vector2 position)
    {
        base.OnPrimaryCancel(position);
        var chargeTime = chargeStartTime < 0 ? 0 : Time.time - chargeStartTime;

        ItemSystem.SetTrigger("Charge End");

        if (chargeTime >= laserWeaponProperty.Frequency)
        {
            ItemSystem.ShootLaser(position, laserWeaponProperty, Mathf.Min(chargeTime, laserWeaponProperty.MaxDuration));
            ItemSystem.SetTrigger("Shoot");

            if (chargeTime >= laserWeaponProperty.MaxDuration)
                AudioElement.PlayOneShot(laserWeaponProperty.FullChargeShotSfx);
            else
                AudioElement.PlayOneShot(laserWeaponProperty.PrematureShotSfx);
        }
        else
        {
            var timeSincelastChargeTime = Time.time - lastChargeTime;
            if (timeSincelastChargeTime > laserWeaponProperty.PrimaryCdr)
            {
                //Debug.LogWarning("Laser charge time too short, no shot fired", this);
                AudioElement.PlayOneShot(laserWeaponProperty.SecondarySound);
            }

        }

        chargeStartTime = -1f;
        isCharging = false;
    }

    public override void OnSecondaryAction(Vector2 position)
    {
        ItemSystem.SetTrigger("Charge End");

        // Prevent sound spam if the charge was cancelled
        var timeSincelastChargeTime = Time.time - lastChargeTime;
        if (timeSincelastChargeTime > laserWeaponProperty.PrimaryCdr)
        {
            //Debug.LogWarning("Laser charge cancelled, no shot fired", this);
            AudioElement.PlayOneShot(laserWeaponProperty.SecondarySound);
        }

        chargeStartTime = -1f;
        isCharging = false;
    }
}
