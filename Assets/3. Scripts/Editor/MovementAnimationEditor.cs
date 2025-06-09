using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MovementAnimation))]
public class MovementAnimationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Reference to the target script
        MovementAnimation movementAnimation = (MovementAnimation)target;
        GUILayout.Space(10);

        // Add buttons
        if (GUILayout.Button("Update Start Position"))
        {
            Undo.RecordObject(movementAnimation, "Update Start Position");
            movementAnimation.UpdateStartPosition();
            EditorUtility.SetDirty(movementAnimation);
        }

        if (GUILayout.Button("Update Coord Position"))
        {
            Undo.RecordObject(movementAnimation, "Update Coord Position");
            movementAnimation.UpdateCoordPosition();
            EditorUtility.SetDirty(movementAnimation);
        }
    }
}
