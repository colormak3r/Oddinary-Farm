using ColorMak3r.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spider : Animal
{
    [Header("Spider Settings")]
    [SerializeField]
    private AxeProperty axeProperty;
    [SerializeField]
    private float burrowPreventMoveDuration = 1f;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private static int STATE_COUNT = 2;
    private float[] baseWeights = new float[] { 1f, 2f };
    private float[] selectedCounts = new float[STATE_COUNT];
    private float[] adjustedWeights = new float[STATE_COUNT];

    private float nextMoveAfterBurrow = -1;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Item.PropertyValue = axeProperty;
        }

    }

    protected override void HandleTransitions()
    {
        if (PreyDetector.CurrentPrey == null)
        {
            // Idle States
            if (Time.time > nextIdleStateChange)
            {
                nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);

                if(currentState is ChasingState || currentState is AttackPrimaryState)
                {
                    if (ShowDebug) Debug.Log("Change state to Thinking State");
                    ChangeState(new ThinkingState(this));
                }
                else
                {
                    float totalWeight = 0f;
                    for (int i = 0; i < STATE_COUNT; i++)
                    {
                        adjustedWeights[i] = baseWeights[i] * (STATE_COUNT / (1f + selectedCounts[i]));
                        totalWeight += adjustedWeights[i];
                    }

                    var rng = Random.Range(0, totalWeight);
                    if (rng < adjustedWeights[0])
                    {
                        if (TimeManager.Main.IsDay)
                        {
                            if (ShowDebug) Debug.Log("Change state to Burrowing State");
                            ChangeState(new BurrowingState(this));
                        }
                        else
                        {
                            if (ShowDebug) Debug.Log("Change state to Roaming State");
                            ChangeState(new RoamingState(this));
                        }                      
                        selectedCounts[0]++;
                    }
                    else
                    {
                        if (ShowDebug) Debug.Log("Change state to Burrowing State");
                        ChangeState(new BurrowingState(this));
                        selectedCounts[1]++;
                    }
                }                
            }
        }
        else
        {
            nextIdleStateChange = 0;
            if (PreyDetector.DistanceToPrey > axeProperty.Range)
            {
                if (currentState is not ChasingState)
                {
                    if (ShowDebug) Debug.Log("Change state to Chasing State");
                    ChangeState(new ChasingState(this));
                }
            }
            else
            {
                if (currentState is not AttackPrimaryState)
                {
                    if (ShowDebug) Debug.Log("Change state to AttackPrimary State");
                    ChangeState(new AttackPrimaryState(this));
                }
            }
        }
    }
}
