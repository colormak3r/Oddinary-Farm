using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PrefabPosition))]
public class PrefabPositionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin property
        EditorGUI.BeginProperty(position, label, property);

        // Draw foldout
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Calculate rects
        float halfWidth = position.width / 2f;
        Rect prefabRect = new Rect(position.x, position.y, halfWidth - 2, position.height);
        Rect posRect = new Rect(position.x + halfWidth + 2, position.y, halfWidth - 2, position.height);

        // Draw fields
        SerializedProperty prefabProp = property.FindPropertyRelative("Prefab");
        SerializedProperty positionProp = property.FindPropertyRelative("Position");

        EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
        EditorGUI.PropertyField(posRect, positionProp, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
