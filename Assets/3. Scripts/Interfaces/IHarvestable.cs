using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHarvestable 
{
    public bool IsHarvestable();

    public void GetHarvested();
}
