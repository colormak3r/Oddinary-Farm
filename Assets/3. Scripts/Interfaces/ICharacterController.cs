using Unity.Netcode.Components;
using UnityEngine;

public interface ICharacterController
{
    public Animator Animator { get; }
    public NetworkAnimator NetworkAnimator { get; }
}
