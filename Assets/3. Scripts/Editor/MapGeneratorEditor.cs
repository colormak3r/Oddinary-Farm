using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector options
        DrawDefaultInspector();

        WorldGenerator script = (WorldGenerator)target;

        /*if (GUILayout.Button("Generate"))
        {
            script.Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            script.Clear();
        }*/
    }
}
