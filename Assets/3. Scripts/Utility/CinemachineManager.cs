using Unity.Cinemachine;
using UnityEngine;

public class CinemachineManager : MonoBehaviour
{
    public static CinemachineManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null)
        {
            Main = this;
        }
        else
        {
            Destroy(gameObject);
        }

        cinemachineCamera = GetComponent<CinemachineCamera>();
    }

    private CinemachineCamera cinemachineCamera;
    public CinemachineCamera CinemachineCamera => cinemachineCamera;
}
