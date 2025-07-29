using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public interface IConsummable
{
    public FoodColor FoodColor { get; }
    public FoodType FoodType { get; }
    public GameObject GameObject { get; }
    public bool CanBeConsumed { get; }
    public void AddToHunterList(HungerStimulus hungerStimulus);
    public void RemoveFromHunterList(HungerStimulus hungerStimulus);

    public bool Consume();
}
