using UnityEngine;
using UnityEngine.UI;

public class GameplayRenderer : MonoBehaviour
{
    public static GameplayRenderer Main { get; private set; }

    [SerializeField]
    private RawImage rawImage;
    public RawImage RawImage => rawImage;
    [SerializeField]
    private Camera uiCamera;
    public Camera UICamera => uiCamera;

    private void Awake()
    {
        Main = this;
    }
}
