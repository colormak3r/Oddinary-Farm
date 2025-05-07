using UnityEngine;

public class Chicken : Animal
{
    [Header("Chicken Settings")]
    [SerializeField]
    private bool isTamed;
    [SerializeField]
    private MinMaxFloat idleStateChangeCdr = new MinMaxFloat { min = 3, max = 5 };
    private float nextIdleStateChange;

    private BehaviourState thinkingState;
    private BehaviourState nibblingState;
    private BehaviourState roamingState;

    private BehaviourState runawayState;
    private BehaviourState seekfoodState;

    private BehaviourState[] idleStates;
    private BehaviourState[] activeStates;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            thinkingState = new ThinkingState(this);
            nibblingState = new NibblingState(this);
            roamingState = new RoamingState(this);

            runawayState = new RunawayState(this);
            seekfoodState = new SeekFoodState(this);

            idleStates = new BehaviourState[] { thinkingState, nibblingState, roamingState };
        }
    }

    protected override void HandleTransitions()
    {
        if (ThreatDetector.CurrentThreat == null)
        {
            if (isTamed)
            {
                if (HungerStimulus && HungerStimulus.IsHungry)
                {
                    if (HungerStimulus.TargetFood == null)
                    {
                        if (currentState != roamingState)
                        {
                            ChangeState(roamingState);
                        }
                    }
                    else
                    {
                        if (currentState != seekfoodState)
                        {
                            ChangeState(seekfoodState);
                        }
                    }
                }
                else
                {
                    if (Time.time > nextIdleStateChange)
                    {
                        var newState = idleStates[Random.Range(0, idleStates.Length)];
                        ChangeState(newState);
                        nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);
                    }
                }
            }
            else
            {
                // Idle States
                if (Time.time > nextIdleStateChange)
                {
                    var newState = idleStates[Random.Range(0, idleStates.Length)];
                    ChangeState(newState);
                    nextIdleStateChange = Time.time + Random.Range(idleStateChangeCdr.min, idleStateChangeCdr.max);
                }
            }
        }
        else
        {
            if (currentState != runawayState) ChangeState(runawayState);
        }

    }

    protected override void OnStateChanged(BehaviourState oldState, BehaviourState newState)
    {
        nextIdleStateChange = 0;
    }
}
