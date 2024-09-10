using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector options
        DrawDefaultInspector();

        MapGenerator script = (MapGenerator)target;

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
