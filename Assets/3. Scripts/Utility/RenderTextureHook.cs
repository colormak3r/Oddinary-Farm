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
        if (displayImage == null) return;
        if (showInEditor) Debug.LogWarning($"{gameObject.name} RenderTextureHook is enabled in the editor. Remember to turn this off before commit to Git", this);

        displayImage.enabled = true;
        displayImage.texture = renderTexture;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Application.isPlaying || displayImage == null || renderTexture == null) return;
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
