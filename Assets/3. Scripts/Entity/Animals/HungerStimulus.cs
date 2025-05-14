using System;
using Unity.Netcode;
using UnityEngine;

[Flags]
public enum FoodType : int
{
    None = 0,
    Grass = 1 << 0,  // 1
    Crop = 1 << 1,   // 2
    Seed = 1 << 2,   // 4
    Insect = 1 << 3, // 8
    Fruit = 1 << 4,  // 16
    Meat = 1 << 5    // 32
}

[Flags]
public enum FoodColor : int
{
    None = 0,
    Green = 1 << 0,   // 1
    Brown = 1 << 1,   // 2
    Red = 1 << 2,     // 4
    Yellow = 1 << 3,  // 8
    White = 1 << 4,   // 16
    Orange = 1 << 5   // 32
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
    [SerializeField]
    private Transform targetFoodTransform;
    private IConsummable targetFood;
    public IConsummable TargetFood => targetFood;

    private float nextHunger;
    private float nextDeath;
    const float StallDelay = 0.1f;
    private float stallStart = -1f;

    private LootGenerator lootGenerator;
    private ContextBubbleUI contextBubbleUI;
    private Rigidbody2D rbody;

    private void Awake()
    {
        contextBubbleUI = GetComponentInChildren<ContextBubbleUI>();
        rbody = GetComponent<Rigidbody2D>();
    }

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

            ShowContextBubbleRpc();

            if (showDebugs) Debug.Log("I'm hungry", this);
        }

        if (isHungry && Time.time > nextDeath)
        {
            GetComponent<EntityStatus>().TakeDamage(100, DamageType.Blunt, Hostility.Neutral, transform);
        }

        if (isHungry && targetFood == null)
        {
            targetFood = ScanForFood();
            targetFoodTransform = targetFood?.Transform;
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
                    HideContextBubbleRpc();
                }
                else
                {
                    targetFood = null;
                }
            }

            var velocity = rbody.linearVelocity;
            if ((velocity.x == 0 || velocity.y == 0) && stallStart < 0f)
                stallStart = Time.time;
            else if (velocity.x != 0 && velocity.y != 0 && stallStart >= 0f)
                stallStart = -1f;

            if (stallStart >= 0f && Time.time - stallStart > StallDelay)
            {
                targetFood = ScanForFood();
                targetFoodTransform = targetFood?.Transform;
                stallStart = -1f;
            }
        }
    }

    private IConsummable ScanForFood()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, foodDetectionRadius, foodLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out IConsummable consummable))
            {
                if (consummable.FoodType.HasFlag(foodType) && consummable.FoodColor.HasFlag(foodColor) && consummable.CanBeConsumed)
                {
                    if (showDebugs) Debug.Log($"Found food: {consummable.Transform.name}", this);
                    return consummable;
                }
            }
        }
        return null;
    }

    [Rpc(SendTo.Everyone)]
    private void ShowContextBubbleRpc()
    {
        contextBubbleUI.Show();
    }

    [Rpc(SendTo.Everyone)]
    private void HideContextBubbleRpc()
    {
        contextBubbleUI.Hide();
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
