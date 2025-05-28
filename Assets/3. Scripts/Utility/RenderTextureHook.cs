using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class RenderTextureHook : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showInEditor = true;
    [SerializeField]
    private RenderTexture renderTexture;
    [SerializeField]
    private RawImage displayImage;

    private void Awake()
    {
        displayImage.enabled = true;
        displayImage.texture = renderTexture;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying) return;
        displayImage.enabled = showInEditor;
        if (showInEditor)
        {
            displayImage.texture = renderTexture;
        }
        else
        {
            displayImage.texture = null;
        }
    }
#endif
}
