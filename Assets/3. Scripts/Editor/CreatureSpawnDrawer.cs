using UnityEngine;
using UnityEditor;

// Custom drawer for CreatureSpawn struct to display prefab and count side by side
[CustomPropertyDrawer(typeof(CreatureSpawn))]
public class CreatureSpawnDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Begin drawing the property
        label = EditorGUI.BeginProperty(position, label, property);

        // Draw the main label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Store and reset indent
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate widths for prefab and spawnCount
        float totalWidth = position.width;
        float prefabWidth = totalWidth * 0.65f;
        float countWidth = totalWidth - prefabWidth - 4f;

        // Rects for each field
        Rect prefabRect = new Rect(position.x, position.y, prefabWidth, position.height);
        Rect countRect = new Rect(position.x + prefabWidth + 4f, position.y, countWidth, position.height);

        // Fetch serialized properties
        var prefabProp = property.FindPropertyRelative("creaturePrefab");
        var countProp = property.FindPropertyRelative("spawnCount");

        // Draw fields without labels
        EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
        EditorGUI.PropertyField(countRect, countProp, GUIContent.none);

        // Restore indent
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Single line height
        return EditorGUIUtility.singleLineHeight;
    }
}
