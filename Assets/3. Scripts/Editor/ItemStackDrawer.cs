using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ItemStack))]
public class ItemStackDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Start the property drawer
        EditorGUI.BeginProperty(position, label, property);

        // Set the amount of padding between elements
        float padding = 5f;

        // Calculate the total width and divide it into three sections for element number, property, and count
        float width = position.width;
        float elementWidth = EditorGUIUtility.labelWidth; // 10% of the width for element number
        float propertyWidth = (width - EditorGUIUtility.labelWidth) * 0.8f; // 60% for the Property field
        float countWidth = (width - EditorGUIUtility.labelWidth) * 0.2f; // 30% for the Count field

        // Get the current property's name (element number)
        string elementLabel = label.text;

        // Get the Property and Count fields
        SerializedProperty propertyField = property.FindPropertyRelative("Property");
        SerializedProperty countField = property.FindPropertyRelative("Count");

        // Draw the element number (label) with padding
        Rect elementRect = new Rect(position.x, position.y + 2, elementWidth, position.height - 2);
        EditorGUI.LabelField(elementRect, elementLabel);

        // Draw the Property field with padding
        Rect propertyRect = new Rect(position.x + elementWidth + padding, position.y + 2, propertyWidth - padding, position.height - 2);
        EditorGUI.PropertyField(propertyRect, propertyField, GUIContent.none);

        // Draw the Count field with padding
        Rect countRect = new Rect(position.x + elementWidth + propertyWidth + 2 * padding, position.y + 2, countWidth - 2 * padding, position.height - 2);
        EditorGUI.PropertyField(countRect, countField, GUIContent.none);

        // End the property drawer
        EditorGUI.EndProperty();
    }
}
