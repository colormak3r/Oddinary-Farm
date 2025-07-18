using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImageAnimator : MonoBehaviour
{
    [Header("Settings")]
    public bool loop = true;
    public int framePerSecond = 24;
    [SerializeField]
    private Sprite[] sprites;

    private int index = 0;
    private float nextFrame;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (!loop || sprites == null || index >= sprites.Length) return;
        if (Time.time < nextFrame) return;
        nextFrame = Time.time + 1f / framePerSecond;

        image.sprite = sprites[index];

        index++;
        if (index >= sprites.Length)
            if (loop) index = 0;
    }

    public void SetSprites(Sprite[] newSprites)
    {
        sprites = newSprites;
        index = 0;
    }
}