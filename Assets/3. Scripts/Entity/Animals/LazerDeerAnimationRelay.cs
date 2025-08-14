using UnityEngine;

public class LazerDeerAnimationRelay : MonoBehaviour
{
    [Header("Laser Deer Animation Settings")]
    [SerializeField]
    private LaserDeer laserDeer;

    private void PlayStepVSFX()
    {
        laserDeer.PlayStepVSFX();
    }
}
