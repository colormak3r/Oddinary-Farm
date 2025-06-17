using UnityEngine;
using UnityEngine.UI;

public class GifController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Image gifImage;

    private void Awake()
    {
        gifImage = GetComponent<Image>();
    }

}
