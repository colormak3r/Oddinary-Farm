/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/26/2025
 * Last Modified:   07/26/2025 (Khoa)
 * Notes:           <write here>
*/

using Unity.Netcode;
using UnityEngine;

public class CrystalSnail : Animal
{
    private BehaviourState roamingState;
    private BehaviourState runawayState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            roamingState = new RoamingState(this);
            runawayState = new RunawayState(this);
        }
    }

    protected override void HandleTransitions()
    {
        if (ThreatDetector.CurrentThreat == null)
        {
            if (currentState != roamingState)
            {
                ChangeState(roamingState);
            }
        }
        else
        {
            if (currentState != runawayState)
            {
                ChangeState(runawayState);
            }
        }
    }
}
