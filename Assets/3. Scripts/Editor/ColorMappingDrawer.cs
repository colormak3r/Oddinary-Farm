using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ColorMapping))]
public class ColorMappingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin Property
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        SerializedProperty colorProp = property.FindPropertyRelative("color");
        SerializedProperty valueProp = property.FindPropertyRelative("value");

        // Create rects
        var colorWidth = position.width * 0.7f;
        Rect colorRect = new Rect(position.x, position.y, colorWidth, position.height);
        Rect valueRect = new Rect(position.x + colorWidth, position.y, position.width - colorWidth, position.height);

        // Draw fields
        EditorGUI.PropertyField(colorRect, colorProp, GUIContent.none);
        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

        // End Property
        EditorGUI.EndProperty();
    }
}
