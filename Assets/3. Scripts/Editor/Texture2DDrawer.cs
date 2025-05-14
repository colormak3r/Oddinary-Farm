using UnityEngine;
using UnityEditor;
using static Codice.CM.Common.CmCallContext;

[CustomPropertyDrawer(typeof(Texture2D))]
public class Texture2DDrawer : PropertyDrawer
{
    private bool showTexture = true; // Foldout toggle

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
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

        EditorGUI.EndProperty();
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
        }

        return height;
    }
}
