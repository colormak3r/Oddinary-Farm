using UnityEngine;
using UnityEditor;
using static Codice.CM.Common.CmCallContext;

[CustomPropertyDrawer(typeof(Texture2D))]
public class Texture2DDrawer : PropertyDrawer
{
    private bool showTexture = true; // Foldout toggle

    /*public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw the foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        showTexture = EditorGUI.Foldout(foldoutRect, showTexture, label);

        // Draw the texture field
        Rect textureFieldRect = new Rect(position.x, foldoutRect.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(textureFieldRect, property, GUIContent.none);

        float currentY = textureFieldRect.yMax + 5;

        // Display the texture preview if expanded
        if (showTexture && property.objectReferenceValue != null)
        {
            Texture2D texture = property.objectReferenceValue as Texture2D;
            float aspectRatio = texture.width == 0 ? 1 : (float)texture.height / texture.width;
            float previewHeight = position.width * aspectRatio;
            Rect previewRect = new Rect(position.x, currentY, position.width, previewHeight);
            EditorGUI.DrawPreviewTexture(previewRect, texture);
            currentY = previewRect.yMax + 5;
        }

        // Check if the target object is a MapGenerator and this is the mapTexture field
        MapGenerator generator = property.serializedObject.targetObject as MapGenerator;
        if (generator != null && property.name == "mapTexture")
        {
            Rect buttonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "Generate Preview"))
            {
                generator.GeneratePreview();
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;
        }

        // Add a button to generate the map
        if (generator != null && property.name == "mapTexture")
        {
            Rect generateButtonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(generateButtonRect, "Randomize Map"))
            {
                generator.RandomizeMap();
                generator.GeneratePreview();
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;
        }

        // Add a button to export the map texture
        if (generator != null && property.name == "mapTexture")
        {
            Rect exportButtonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(exportButtonRect, "Export Texture as PNG"))
            {
                string path = EditorUtility.SaveFilePanel("Save Texture As PNG", Application.dataPath, "mapTexture", "png");
                if (!string.IsNullOrEmpty(path))
                {
                    Texture2D texture = property.objectReferenceValue as Texture2D;

                    // Ensure it's readable
                    RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
                    Graphics.Blit(texture, rt);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = rt;

                    Texture2D readableTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    readableTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    readableTex.Apply();

                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);

                    // Save as PNG
                    byte[] bytes = readableTex.EncodeToPNG();
                    System.IO.File.WriteAllBytes(path, bytes);
                    AssetDatabase.Refresh();

                    Object.DestroyImmediate(readableTex);
                }
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;
        }

        EditorGUI.EndProperty();
    }*/
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Foldout and property field
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        bool wasExpanded = showTexture;
        showTexture = EditorGUI.Foldout(foldoutRect, showTexture, label);

        // 2. Texture object field (wrap with BeginProperty/EndProperty)
        Rect textureFieldRect = new Rect(position.x, foldoutRect.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.BeginProperty(textureFieldRect, label, property);
        EditorGUI.PropertyField(textureFieldRect, property, GUIContent.none);
        EditorGUI.EndProperty();

        float currentY = textureFieldRect.yMax + 5;

        // 3. Texture preview
        if (showTexture && property.objectReferenceValue != null)
        {
            Texture2D texture = property.objectReferenceValue as Texture2D;
            float aspectRatio = texture.width == 0 ? 1 : (float)texture.height / texture.width;
            float previewHeight = position.width * aspectRatio;
            Rect previewRect = new Rect(position.x, currentY, position.width, previewHeight);
            EditorGUI.DrawPreviewTexture(previewRect, texture);
            currentY = previewRect.yMax + 5;
        }

        // 4. Special buttons if this is the mapTexture field of MapGenerator
        MapGenerator generator = property.serializedObject.targetObject as MapGenerator;
        if (generator != null && property.name == "mapTexture")
        {
            // Button: Generate Preview
            Rect previewButtonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(previewButtonRect, "Generate Preview"))
            {
                generator.GeneratePreview();
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;

            // Button: Randomize Map
            Rect randomizeButtonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(randomizeButtonRect, "Randomize Map"))
            {
                generator.RandomizeMap();
                generator.GeneratePreview();
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;

            // Button: Export PNG
            Rect exportButtonRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(exportButtonRect, "Export Texture as PNG"))
            {
                ExportTextureAsPNG(property.objectReferenceValue as Texture2D);
            }
            currentY += EditorGUIUtility.singleLineHeight + 5;
        }
    }

    private void ExportTextureAsPNG(Texture2D texture)
    {
        if (texture == null) return;

        string path = EditorUtility.SaveFilePanel("Save Texture As PNG", Application.dataPath + "/Map Texture", texture.name, "png");
        if (string.IsNullOrEmpty(path)) return;

        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTex = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        byte[] bytes = readableTex.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        Object.DestroyImmediate(readableTex);

        Debug.Log($"Texture exported to: {path}");
    }



    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing; // Base height (foldout + texture field)

        if (showTexture && property.objectReferenceValue != null)
        {
            Texture2D texture = (Texture2D)property.objectReferenceValue;
            float inspectorWidth = EditorGUIUtility.currentViewWidth;
            var aspectRatio = texture.width == 0 ? 0 : texture.height / texture.width;
            height += aspectRatio * EditorGUIUtility.currentViewWidth - EditorGUIUtility.singleLineHeight;
        }

        // If this is the mapTexture field in MapGenerator, add height for the button.
        MapGenerator generator = property.serializedObject.targetObject as MapGenerator;
        if (generator != null && property.name == "mapTexture")
        {
            height += EditorGUIUtility.singleLineHeight + 5;
            height += EditorGUIUtility.singleLineHeight + 5;
            height += EditorGUIUtility.singleLineHeight + 5;
        }

        return height;
    }
}
