using UnityEditor;
using UnityEngine;

public class Texture2DViewer : EditorWindow
{
    private Texture2D texture;
    private Vector2 scrollPosition;
    private float zoom = 1f;

    [MenuItem("Tools/Texture2D Viewer")]
    public static void ShowWindow()
    {
        GetWindow<Texture2DViewer>("Texture2D Viewer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select a Texture2D", EditorStyles.boldLabel);

        texture = (Texture2D)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), false);
        zoom = EditorGUILayout.Slider("Zoom", zoom, 0.1f, 10f);

        if (texture != null)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                Rect textureRect = GUILayoutUtility.GetRect(texture.width * zoom, texture.height * zoom, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                EditorGUI.DrawPreviewTexture(textureRect, texture, null, ScaleMode.ScaleToFit);
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
