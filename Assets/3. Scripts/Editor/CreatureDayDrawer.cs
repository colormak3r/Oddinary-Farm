using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CreatureDay))]
public class CreatureDayDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Draw the label
        var labelTexts = label.text.Split(' ');
        EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), "Day " + (Int32.Parse(labelTexts[1]) + 1));

        // Gather the properties
        SerializedProperty waves = property.FindPropertyRelative("creatureWaves");
        int waveCount = waves.arraySize;
        int lineCount = 1;
        string summary = "";
        for (int i = 0; i < waveCount; i++)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(i);
            SerializedProperty spawnHour = wave.FindPropertyRelative("spawnHour");
            SerializedProperty creatureSpawns = wave.FindPropertyRelative("creatureSpawns");
            summary += $"Wave {i + 1} - {spawnHour.intValue}:00\n";
            lineCount += 1 + creatureSpawns.arraySize;

            for (int j = 0; j < creatureSpawns.arraySize; j++)
            {
                SerializedProperty spawn = creatureSpawns.GetArrayElementAtIndex(j);
                SerializedProperty creaturePrefab = spawn.FindPropertyRelative("creaturePrefab");
                SerializedProperty spawnCount = spawn.FindPropertyRelative("spawnCount");

                summary += $"   {spawnCount.intValue}x {creaturePrefab.objectReferenceValue.name}\n";
            }
        }
        EditorGUI.LabelField(new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight / 2f, position.width, EditorGUIUtility.singleLineHeight * lineCount), summary);
        Rect wavesRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * lineCount, position.width, EditorGUI.GetPropertyHeight(waves, true));
        EditorGUI.PropertyField(wavesRect, waves, GUIContent.none, true);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty waves = property.FindPropertyRelative("creatureWaves");
        int waveCount = waves.arraySize;

        int lineCount = 1; // Initial label
        for (int i = 0; i < waveCount; i++)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(i);
            SerializedProperty creatureSpawns = wave.FindPropertyRelative("creatureSpawns");
            lineCount += 1 + creatureSpawns.arraySize;
        }

        float summaryHeight = EditorGUIUtility.singleLineHeight * lineCount;
        float wavesHeight = EditorGUI.GetPropertyHeight(waves, true);

        return summaryHeight + wavesHeight;
    }
}
