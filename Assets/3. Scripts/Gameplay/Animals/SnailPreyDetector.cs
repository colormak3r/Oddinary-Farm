using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnailPreyDetector : PreyDetector
{
    protected override bool ValidateValidPrey(Transform prey)
    {
        return prey.GetComponent<Plant>().IsHarvestable; 
    }
}
