using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField]
    private PlayerController playerController;

    public AnimationMode ChopAnimationMode;

    public void Chop()
    {
        playerController.Chop(ChopAnimationMode);
    }
}
