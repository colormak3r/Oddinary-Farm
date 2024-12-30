using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SelectorModifier))]
public class SelectorModifierEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector UI
        DrawDefaultInspector();
        GUILayout.Space(10);

        // Reference to the target script
        SelectorModifier selectorModifier = (SelectorModifier)target;

        // Add a Test button
        if (GUILayout.Button("Test"))
        {
            selectorModifier.Test();
        }

        // Add a Reset button
        if (GUILayout.Button("Reset"))
        {
            selectorModifier.Reset();
        }
    }
}