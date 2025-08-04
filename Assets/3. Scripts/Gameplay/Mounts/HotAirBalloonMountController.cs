/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/23/2025
 * Last Modified:   08/03/2025 (Khoa)
 * Notes:           Handles the mounting and movement of the hot air balloon
*/

using System;
using Unity.Netcode;
using UnityEngine;

public class HotAirBalloonMountController : MountController
{
    private NetworkVariable<bool> HasTakenOff = new NetworkVariable<bool>(false);

    private bool directionSet = false;
    protected override void Update()
    {
        base.Update();
        // Anticipate ownership change when taking off
        // If ownership changed and direction is not set, set the direction to up once
        if (IsOwner && !directionSet && HasTakenOff.Value)
        {
            mountMovement.SetDirection(new Vector2(0f, 1f));
            directionSet = true;
        }
    }

    public void TakeOffOnServer()
    {
        if (!IsServer) return;
        HasTakenOff.Value = true;
    }

    public override void Move(Vector2 direction)
    {
        // Player can only move horizontally while in the hot air balloon
        Vector2 totalDir = new Vector2(direction.x, 1f);
        mountMovement.SetDirection(totalDir);
        if (showDebugs) Debug.Log($"Player is Moving Balloon = {totalDir}");
    }
}
