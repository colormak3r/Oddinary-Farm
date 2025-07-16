// Put this in the same file or in an Editor folder
// Make sure WeatherData is the class you store in the array
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeatherData))]
public class WeatherDataDrawer : PropertyDrawer
{
    // ---------- GUI ---------- //
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Extract the array index:  "weatherList.Array.data[3]"  3
        int index = GetArrayIndex(property);

        // Change the header text
        label = new GUIContent($"Day {index + 1}");

        // Draw the fold-out header
        Rect foldoutRect = new Rect(position.x,
                                    position.y,
                                    position.width,
                                    EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (!property.isExpanded)
            return;                     // collapsed  nothing else to draw

        // Start drawing the inside, indented
        EditorGUI.indentLevel++;

        // Line spacing helper
        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        Rect line = new Rect(position.x,
                             position.y + lineH + spacing,
                             position.width,
                             lineH);

        // Field 1 – IsThunderStorm (checkbox)
        SerializedProperty isThunderStorm = property.FindPropertyRelative("IsThunderStorm");
        EditorGUI.PropertyField(line, isThunderStorm);

        // Field 2 – RainDuration (MinMaxInt or int / slider, whatever you use)
        line.y += lineH + spacing;
        SerializedProperty rainDuration = property.FindPropertyRelative("RainDuration");
        EditorGUI.PropertyField(line, rainDuration);

        EditorGUI.indentLevel--;
    }

    // ---------- height ---------- //
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;

        // One line if collapsed, three lines (header + 2 fields + spacing) if expanded
        return property.isExpanded
               ? lineH * 3 + spacing * 2
               : lineH;
    }

    // ---------- helper ---------- //
    private int GetArrayIndex(SerializedProperty property)
    {
        var match = System.Text.RegularExpressions.Regex.Match(property.propertyPath, @"\[(\d+)\]");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int i))
            return i;
        return -1;
    }
}
