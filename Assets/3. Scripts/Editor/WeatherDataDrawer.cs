using UnityEngine;
using UnityEditor;

public class WeatherDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        float totalWidth = position.width;
        float boolWidth = totalWidth * 0.3f;
        float durationWidth = totalWidth - boolWidth;

        Rect boolRect = new Rect(position.x, position.y, boolWidth, EditorGUIUtility.singleLineHeight);
        Rect durationRect = new Rect(position.x + boolWidth + 4, position.y, durationWidth - 4, EditorGUIUtility.singleLineHeight);

        SerializedProperty isThunderStormProp = property.FindPropertyRelative("IsThunderStorm");
        SerializedProperty rainDurationProp = property.FindPropertyRelative("RainDuration");

        EditorGUI.PropertyField(boolRect, isThunderStormProp, GUIContent.none);
        EditorGUI.PropertyField(durationRect, rainDurationProp, GUIContent.none);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Ensure enough space if MinMaxInt is drawn on multiple lines
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("RainDuration")) + 2;
    }
}
