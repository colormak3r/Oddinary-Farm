/*
 * Created By:      Khoa Nguyen
 * Date Created:    --/--/----
 * Last Modified:   07/02/2025 (Khoa)
 * Notes:           <write here>
*/

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssetManager))]
public class AssetManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AssetManager script = (AssetManager)target;

        // Info label at the top
        EditorGUILayout.HelpBox("Fetch Assets will automatically run everytime the game is played in the editor. Manual fetch is needed if asset is added without playing.", MessageType.Warning);

        if (GUILayout.Button("Fetch Assets"))
        {
            script.FetchAssets();
        }

        // Draw the default inspector options
        DrawDefaultInspector();
    }
}
