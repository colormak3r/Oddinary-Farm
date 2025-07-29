/*
 * Created By:      Khoa Nguyen
 * Date Created:    07/25/2025
 * Last Modified:   07/05/2025 (Khoa)
 * Notes:           <write here>
*/


using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RadioEvent))]
public class RadioEventDrawer : PropertyDrawer
{
    const float k_Spacing = 4f;      // vertical padding between rows

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Grab sub-fields
        SerializedProperty dayProp = property.FindPropertyRelative("day");
        SerializedProperty hourProp = property.FindPropertyRelative("hour");
        SerializedProperty msgProp = property.FindPropertyRelative("message");

        // Change the element’s fold-out label (e.g. “Day 3 Hour 14”)
        label.text = $"Day {dayProp.intValue} Hour {hourProp.intValue}";

        // Draw the fold-out prefix and get a rect for the content that follows it
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        float lineHeight = EditorGUIUtility.singleLineHeight;

        // --- DAY & HOUR on one line ----------------------------------------
        float halfWidth = (position.width - 4) * 0.5f;   // 4-pixel gap between the two fields
        Rect dayRect = new Rect(position.x, position.y, halfWidth, lineHeight);
        Rect hourRect = new Rect(position.x + halfWidth + 4, position.y, halfWidth, lineHeight);

        EditorGUI.PropertyField(dayRect, dayProp, GUIContent.none);
        EditorGUI.PropertyField(hourRect, hourProp, GUIContent.none);

        // --- MESSAGE field --------------------------------------------------
        Rect msgRect = new Rect(position.x,
                                position.y + k_Spacing,
                                position.width,
                                EditorGUI.GetPropertyHeight(msgProp, true));

        EditorGUI.PropertyField(msgRect, msgProp, GUIContent.none, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 1 line for day/hour + spacing + full height of the multi-line message box
        float msgHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("message"), true);
        return k_Spacing + msgHeight + k_Spacing * 2;
    }
}
