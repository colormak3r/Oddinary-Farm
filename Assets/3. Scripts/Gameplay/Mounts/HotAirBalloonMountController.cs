using UnityEngine;
using static Unity.VisualScripting.Member;

public class HotAirBalloonMountController : MountController
{
    protected override void HandleOnMount(Transform source)
    {
        if (source.TryGetComponent<HotAirBalloonController>(out var controller))
        {
            controller.SetControl(true);       // Disable Player controls
        }
    }

    protected override void HandleOnDismount(Transform source)
    {
        if (source.TryGetComponent<HotAirBalloonController>(out var controller))
        {
            controller.SetControl(false);       // Disable Player controls
        }
    }

    protected override void Move(Vector2 motion, float deltaTime)
    {
        Debug.Log("Player is Moving Balloon");
    }
}
