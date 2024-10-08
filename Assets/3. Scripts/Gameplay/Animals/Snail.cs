using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;



public class Snail : Animal
{
    private PreyDetector preyDetector;

    private List<IAnimalBehavior> behaviours = new List<IAnimalBehavior>();
    private IdleBehaviour idleBehaviour;
    private HuntBehaviour huntBehaviour;

    private IAnimalBehavior currentBehavior;

    private void Awake()
    {
        preyDetector = GetComponent<PreyDetector>();

        idleBehaviour = GetComponent<IdleBehaviour>();
        huntBehaviour = GetComponent<HuntBehaviour>();

        behaviours.Add(idleBehaviour);
        behaviours.Add(huntBehaviour);
    }


    private void OnEnable()
    {
        preyDetector.OnPreyDetected.AddListener(HandlePreyDetected);
        preyDetector.OnPreyExited.AddListener(HandlePreyExited);
    }

    private void OnDisable()
    {
        preyDetector.OnPreyDetected.RemoveAllListeners();
    }


    private void HandlePreyDetected(Transform prey)
    {
        currentBehavior.ExitBehavior();
        currentBehavior = huntBehaviour;
        currentBehavior.StartBehavior();
    }


    private void HandlePreyExited(Transform prey)
    {
        currentBehavior.ExitBehavior();
        currentBehavior = idleBehaviour;
        currentBehavior.StartBehavior();
    }


    protected override void Update()
    {
        base.Update();

        if (!IsServer) return;

        if(currentBehavior == null)
        {
            currentBehavior = idleBehaviour;
        }      

        currentBehavior.ExecuteBehavior();
    }
}
