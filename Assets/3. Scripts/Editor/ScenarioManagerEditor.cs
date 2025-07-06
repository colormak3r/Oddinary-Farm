/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScenarioManager))]
public class ScenarioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Make sure internal value of Current Scenario is set to None and Override Settings is false before build", MessageType.Warning);

        // Draw the default inspector options
        DrawDefaultInspector();
    }
}
