using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public enum IdleStrategy
{
    Wanderer,
    Burrower
}

public enum IdleState
{
    Roaming,
    Burrowing,
    Thinking,
    Nibbling
}

[System.Serializable]
public class IdleStateProbability
{
    public IdleStateData Data;
    [Range(0, 1)]
    public float BaseProbability; // Renamed for clarity
    [HideInInspector]
    public int SelectionCount;

    // Method to get adjusted probability
    public float GetAdjustedProbability()
    {
        // Example adjustment: decrease probability as selection count increases
        // You can customize this formula as needed
        float adjustmentFactor = 1f / (1f + SelectionCount);
        return BaseProbability * adjustmentFactor;
    }
}

public class BehaviorStateData : ScriptableObject
{
    [Header("Behaviour Settings")]
    [SerializeField]
    private MinMaxFloat duration;

    protected float stateTimer;

    public MinMaxFloat Duration => duration;

    public virtual void Enter(IdleBehaviour idleBehaviour)
    {
        stateTimer = 0f;
        if (idleBehaviour.ShowDebug) Debug.Log($"{idleBehaviour.gameObject.name} started {name}.");
    }

    public virtual void Execute(IdleBehaviour idleBehaviour)
    {
        stateTimer += Time.deltaTime;
    }

    public virtual void Exit(IdleBehaviour idleBehaviour)
    {
        if (idleBehaviour.ShowDebug) Debug.Log($"{idleBehaviour.gameObject.name} ended {name}.");
    }
}

public class IdleStateData : BehaviorStateData
{

}

public class IdleBehaviour : MonoBehaviour, IAnimalBehavior
{
    [Header("General Settings")]
    [SerializeField]
    private IdleStrategy idleStrategy;
    [SerializeField]
    private IdleStateProbability[] idleStateProbabilities;
    [SerializeField]
    private int resetThreshold = 10; // Number of state changes before reset
    private int stateChangeCounter = 0;

    [Header("Debugs")]
    [SerializeField]
    private bool showDebug = false;
    [SerializeField]
    private IdleStateData currentState;

    private EntityMovement entityMovement;
    private Animator animator;

    public bool ShowDebug => showDebug;
    public EntityMovement EntityMovement => entityMovement;
    public Animator Animator => animator;

    private void Start()
    {
        entityMovement = GetComponent<EntityMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    public void StartBehavior()
    {
        
    }

    public void ExecuteBehavior()
    {
        if (currentState == null)
        {
            ChangeState(idleStateProbabilities[0].Data);
        }

        currentState.Execute(this);
    }

    public void ExitBehavior()
    {
        currentState?.Exit(this);
        currentState = null;
    }

    public void ChooseNextState()
    {
        // Calculate weights (higher weight for lower selection count)
        float[] weights = new float[idleStateProbabilities.Length];
        float totalWeight = 0f;

        for (int i = 0; i < idleStateProbabilities.Length; i++)
        {
            // Example: Weight decreases as SelectionCount increases
            weights[i] = idleStateProbabilities[i].BaseProbability / (1 + idleStateProbabilities[i].SelectionCount);
            totalWeight += weights[i];
        }

        // Normalize weights
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= totalWeight;
        }

        // Perform weighted random selection
        float rng = Random.value;
        float cumulative = 0f;
        bool stateSelected = false;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (rng <= cumulative)
            {
                ChangeState(idleStateProbabilities[i].Data);
                idleStateProbabilities[i].SelectionCount++;
                stateSelected = true;
                break;
            }
        }

        if (!stateSelected) ChooseNextState();

        // Handle reset if needed
        stateChangeCounter++;
        if (stateChangeCounter >= resetThreshold)
        {
            ResetSelectionCounts();
            stateChangeCounter = 0;
        }
    }

    private void ResetSelectionCounts()
    {
        foreach (var stateProb in idleStateProbabilities)
        {
            stateProb.SelectionCount = 0;
        }

        if (showDebug) Debug.Log("Selection counts have been reset.");
    }


    public void ChangeState(IdleStateData newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }
}
