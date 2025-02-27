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
    }
}
