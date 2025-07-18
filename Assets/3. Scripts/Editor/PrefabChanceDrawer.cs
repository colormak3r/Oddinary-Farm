using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PrefabChance))]
public class PrefabChanceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var prefabProp = property.FindPropertyRelative("prefab");
        var chanceProp = property.FindPropertyRelative("chance");
        var maxCountProp = property.FindPropertyRelative("maxCount");

        float totalWidth = position.width;
        float height = position.height;

        float prefabWidth = totalWidth * 0.5f;
        float chanceWidth = totalWidth * 0.25f;
        float countWidth = totalWidth * 0.25f;

        float x = position.x;

        var prefabRect = new Rect(x, position.y, prefabWidth, height);
        x += prefabWidth;

        var chanceRect = new Rect(x, position.y, chanceWidth, height);
        x += chanceWidth;

        var maxCountRect = new Rect(x, position.y, countWidth, height);

        EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
        EditorGUI.PropertyField(chanceRect, chanceProp, GUIContent.none);
        EditorGUI.PropertyField(maxCountRect, maxCountProp, GUIContent.none);

        EditorGUI.EndProperty();
    }
}
