using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Reference to the target script
        ItemSpawner spawner = (ItemSpawner)target;

        // Draw the itemPrefab and itemProperties fields
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemProperties"), true);

        // Begin a horizontal group for spawnIndex and Spawn button
        EditorGUILayout.BeginHorizontal();

        // Draw the spawnIndex field without a label
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnIndex"));

        // Add some spacing between the field and the button
        GUILayout.Space(10);

        // Draw the Spawn button
        if (GUILayout.Button("Spawn"))
        {
            spawner.Spawn();
        }

        // End the horizontal group
        EditorGUILayout.EndHorizontal();

        // Draw the Spawn All button
        if (GUILayout.Button("Spawn All"))
        {
            spawner.SpawnAll();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
