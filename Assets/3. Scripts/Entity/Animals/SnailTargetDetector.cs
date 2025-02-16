using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnailTargetDetector : TargetDetector
{
    protected override bool ValidateValidTarget(Transform target, out EntityStatus targetStatus)
    {
        return base.ValidateValidTarget(target, out targetStatus) && target.GetComponent<Plant>().IsHarvestable;
    }
}
