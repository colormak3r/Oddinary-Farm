using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BlendUnit))]
public class BlendUnitDrawer : PropertyDrawer
{
    private const int GridSize = 3; // 3x3 grid
    private const int TotalIBools = GridSize * GridSize; // Total number of IBool elements

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Find properties
        SerializedProperty spriteProp = property.FindPropertyRelative("Sprite");
        SerializedProperty neighborsProp = property.FindPropertyRelative("Neighbors");

        // Ensure neighbors array has 9 elements
        if (neighborsProp.arraySize != TotalIBools)
        {
            neighborsProp.arraySize = TotalIBools;
        }

        // Calculate dimensions
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float spriteSize = GridSize * lineHeight + (GridSize - 1) * spacing;
        float fieldWidth = 2 * spriteSize;
        float labelHeight = lineHeight;
        float totalHeight = labelHeight + spriteSize + spacing;

        // Positions for Sprite, Preview, and Neighbors
        Rect spriteLabelRect = new Rect(position.x, position.y, fieldWidth, labelHeight);
        Rect spriteFieldRect = new Rect(position.x, position.y + labelHeight + spacing, fieldWidth, spriteSize);

        Rect previewLabelRect = new Rect(position.x + fieldWidth + 2 * spacing, position.y, fieldWidth, labelHeight);
        Rect previewFieldRect = new Rect(position.x + fieldWidth + 2 * spacing, position.y + labelHeight + spacing, fieldWidth, spriteSize);

        Rect neighborsLabelRect = new Rect(position.x + 2 * (fieldWidth + 2 * spacing), position.y, fieldWidth, labelHeight);
        Rect neighborsFieldRect = new Rect(position.x + 2 * (fieldWidth + 2 * spacing), position.y + labelHeight + spacing, fieldWidth, spriteSize);

        // Draw Sprite label and field
        EditorGUI.LabelField(spriteLabelRect, "Sprite");
        EditorGUI.PropertyField(spriteFieldRect, spriteProp, GUIContent.none);

        // Draw Preview label and image
        EditorGUI.LabelField(previewLabelRect, "Preview");
        Texture2D texture = AssetPreview.GetAssetPreview(spriteProp.objectReferenceValue);
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            GUI.Box(previewFieldRect, "");
            GUI.DrawTexture(previewFieldRect, texture, ScaleMode.ScaleToFit);
        }

        // Draw Neighbors label and grid
        EditorGUI.LabelField(neighborsLabelRect, "Neighbors");
        DrawIBoolGrid(neighborsFieldRect, neighborsProp);
    }

    private void DrawIBoolGrid(Rect rect, SerializedProperty neighborsProp)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float buttonSize = lineHeight;

        for (int i = 0; i < TotalIBools; i++)
        {
            int row = i / GridSize;
            int col = i % GridSize;

            Rect buttonRect = new Rect(
                rect.x + col * (buttonSize + spacing),
                rect.y + row * (buttonSize + spacing),
                buttonSize,
                buttonSize
            );

            SerializedProperty iboolProp = neighborsProp.GetArrayElementAtIndex(i);

            string displayString = ((IBool)iboolProp.enumValueIndex).ToSymbol();

            if (GUI.Button(buttonRect, displayString))
            {
                iboolProp.enumValueIndex = (iboolProp.enumValueIndex + 1) % System.Enum.GetNames(typeof(IBool)).Length;
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float spriteSize = GridSize * lineHeight + (GridSize - 1) * spacing;
        return lineHeight + spriteSize + spacing;
    }
}
