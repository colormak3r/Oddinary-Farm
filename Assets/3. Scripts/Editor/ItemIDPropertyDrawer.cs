using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ItemID))]
public class ItemIDPropertyDrawer : PropertyDrawer
{
    // Constants for layout
    private const float Padding = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin property drawing
        EditorGUI.BeginProperty(position, label, property);

        // Draw the label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't indent
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        float idWidth = position.width / 4 - Padding;
        Rect idRect = new Rect(position.x, position.y, idWidth, position.height);
        Rect itemPropertyRect = new Rect(position.x + idWidth + Padding, position.y, position.width - idWidth, position.height);

        // Find the SerializedProperties
        SerializedProperty idProp = property.FindPropertyRelative("id");
        SerializedProperty itemPropertyProp = property.FindPropertyRelative("itemProperty");

        // Draw fields
        EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
        EditorGUI.PropertyField(itemPropertyRect, itemPropertyProp, GUIContent.none);

        // Restore indent
        EditorGUI.indentLevel = indent;

        // End property drawing
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Return single line height
        return EditorGUIUtility.singleLineHeight;
    }
}
