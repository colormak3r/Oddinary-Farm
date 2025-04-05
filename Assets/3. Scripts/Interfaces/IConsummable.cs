using UnityEngine;

public interface IConsummable
{
    public FoodColor FoodColor { get; }
    public FoodType FoodType { get; }
    public Transform Transform { get; }
    public bool CanBeConsumed { get; }

    public bool Consume();
}
