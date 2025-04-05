using System;
using Unity.Netcode;
using UnityEngine;

[Flags]
public enum FoodType : byte
{
    Grass,
    Crop,
    Seed,
    Insect,
    Fruit,
    Meat
}

[Flags]
public enum FoodColor : byte
{
    Green,
    Brown,
    Red,
    Yellow,
    White,
    Orange,
}

public class HungerStimulus : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float hourTilHunger = 6f;
    [SerializeField]
    private float hourTilDeath = 12f;
    [SerializeField]
    private float foodDetectionRadius = 10f;
    [SerializeField]
    private float foodConsumptionRadius = 0.5f;
    [SerializeField]
    private FoodType foodType;
    [SerializeField]
    private FoodColor foodColor;
    [SerializeField]
    private LayerMask foodLayer;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebugs;
    [SerializeField]
    private bool showGizmos;
    [SerializeField]
    private bool isHungry = false;
    public bool IsHungry => isHungry;
    [SerializeField]
    private bool isDying = false;
    public bool IsDying => isDying;
    private IConsummable targetFood;
    public IConsummable TargetFood => targetFood;

    private float nextHunger;
    private float nextDeath;

    private LootGenerator lootGenerator;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            nextHunger = Time.time + hourTilHunger * TimeManager.Main.HourDuration;
            lootGenerator = GetComponent<LootGenerator>();
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (!isHungry && Time.time > nextHunger)
        {
            isHungry = true;
            nextDeath = Time.time + hourTilDeath * TimeManager.Main.HourDuration;
            isDying = true;
            if (showDebugs) Debug.Log("I'm hungry", this);
        }

        if (isHungry && Time.time > nextDeath)
        {
            GetComponent<EntityStatus>().TakeDamage(100, DamageType.Blunt, Hostility.Neutral, transform);
        }

        if (isHungry && targetFood == null)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, foodDetectionRadius, foodLayer);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out IConsummable consummable))
                {
                    if (consummable.FoodType.HasFlag(foodType) && consummable.FoodColor.HasFlag(foodColor) && consummable.CanBeConsumed)
                    {
                        if(showDebugs) Debug.Log($"Found food: {consummable.Transform.name}", this);
                        targetFood = consummable;
                        break;
                    }
                }
            }
        }

        if (targetFood != null)
        {
            if (Vector2.Distance(transform.position, targetFood.Transform.position) < foodConsumptionRadius)
            {
                if (targetFood.Consume())
                {
                    targetFood = null;
                    isHungry = false;
                    isDying = false;
                    nextHunger = Time.time + hourTilHunger * TimeManager.Main.HourDuration;
                    lootGenerator.DropLoot();
                }
                else
                {
                    targetFood = null;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);

        if (targetFood != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetFood.Transform.position, foodConsumptionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetFood.Transform.position);
        }
    }
}
