using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SpawnerPosition))]
public class SpawnerPositionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Calculate rects
        float thirdWidth = position.width / 3f;
        Rect spawnerRect = new Rect(position.x, position.y, thirdWidth - 2, position.height);
        Rect posRect = new Rect(position.x + thirdWidth + 2, position.y, thirdWidth - 2, position.height);
        Rect offsetRect = new Rect(position.x + 2 * thirdWidth + 4, position.y, thirdWidth - 2, position.height);

        // Draw fields
        SerializedProperty spawnerProp = property.FindPropertyRelative("SpawnerProperty");
        SerializedProperty positionProp = property.FindPropertyRelative("Position");
        SerializedProperty offsetProp = property.FindPropertyRelative("Offset");

        EditorGUI.PropertyField(spawnerRect, spawnerProp, GUIContent.none);
        EditorGUI.PropertyField(posRect, positionProp, GUIContent.none);
        EditorGUI.PropertyField(offsetRect, offsetProp, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
