using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InventorySlot))]
public class InventorySlotDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        float itemWidth = position.width * 0.8f;
        float countWidth = position.width * 0.2f;

        var itemRect = new Rect(position.x, position.y, itemWidth, position.height);
        var countRect = new Rect(position.x + itemWidth + 2, position.y, countWidth - 2, position.height);

        EditorGUI.PropertyField(itemRect, property.FindPropertyRelative("Item"), GUIContent.none);
        EditorGUI.PropertyField(countRect, property.FindPropertyRelative("Count"), GUIContent.none);

        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}
