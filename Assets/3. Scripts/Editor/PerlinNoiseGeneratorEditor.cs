using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PerlinNoiseGenerator))]
public class PerlinNoiseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the target script
        PerlinNoiseGenerator generator = (PerlinNoiseGenerator)target;

        // Default inspector
        DrawDefaultInspector();

        // Add a space and a button
        EditorGUILayout.Space();
        if (GUILayout.Button("Randomize Map"))
        {
            Undo.RecordObject(generator, "Randomize Map");

            generator.RandomizeMap();
            generator.GeneratePreview();

            EditorUtility.SetDirty(generator);
        }
    }
}
