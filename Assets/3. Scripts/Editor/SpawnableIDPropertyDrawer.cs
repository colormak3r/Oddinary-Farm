using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SpawnableID))]
public class SpawnableIDPropertyDrawer : PropertyDrawer
{
    // Constants for layout
    private const float Padding = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin property drawing
        EditorGUI.BeginProperty(position, label, property);

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't indent child fields
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects for fields
        float totalWidth = position.width;
        float idWidth = totalWidth / 4 - Padding;
        float prefabWidth = totalWidth * 3 / 4;

        Rect idRect = new Rect(position.x, position.y, idWidth, position.height);
        Rect prefabRect = new Rect(position.x + idWidth + Padding, position.y, prefabWidth, position.height);

        // Find the SerializedProperties
        SerializedProperty idProp = property.FindPropertyRelative("id");
        SerializedProperty prefabProp = property.FindPropertyRelative("prefab");

        // Draw fields without labels
        EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
        EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);

        // Restore indent
        EditorGUI.indentLevel = indent;

        // End property drawing
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Return the standard property height
        return EditorGUIUtility.singleLineHeight;
    }
}
