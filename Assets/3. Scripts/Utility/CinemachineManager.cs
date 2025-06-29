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

        iCamera = GetComponent<CinemachineCamera>();
    }

    private CinemachineCamera iCamera;
    public CinemachineCamera Camera => iCamera;
}
