using UnityEngine;
using ColorMak3r.Utility;

public class Snail : Animal
{
    [Header("Snail Settings")]
    [SerializeField]
    private HandProperty handProperty;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private static int STATE_COUNT = 3;
    private float[] selectedCounts = new float[STATE_COUNT];
    private float[] adjustedWeights = new float[STATE_COUNT];

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
            Item.PropertyValue = handProperty;
    }

    protected override void HandleTransitions()
    {
        if (PreyDetector.CurrentPrey == null)
        {
            // Idle States
            if (Time.time > nextIdleStateChange)
            {
                nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);

                float totalWeight = 0f;
                for (int i = 0; i < STATE_COUNT; i++)
                {
                    adjustedWeights[i] = (float)STATE_COUNT / (1f + selectedCounts[i]);
                    totalWeight += adjustedWeights[i];
                }

                var rng = Random.Range(0, totalWeight);
                if (rng < adjustedWeights[0])
                {
                    if (currentState is not ThinkingState) ChangeState(new ThinkingState(this));
                    selectedCounts[0]++;
                }
                else if (rng < adjustedWeights.SumUpTo(1))
                {
                    if (currentState is not NibblingState) ChangeState(new NibblingState(this));
                    selectedCounts[1]++;
                }
                else
                {
                    if (currentState is not RoamingState) ChangeState(new RoamingState(this));
                    selectedCounts[2]++;
                }
            }
        }
        else
        {
            nextIdleStateChange = 0;
            if (PreyDetector.DistanceToPrey > handProperty.Range)
            {
                if (currentState is not ChasingState) ChangeState(new ChasingState(this));
            }
            else
            {
                if (currentState is not AttackPrimaryState) ChangeState(new AttackPrimaryState(this));
            }
        }
    }
}


