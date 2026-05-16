using DefaultNamespace;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelCreator))]
public class LevelCreatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector first
        DrawDefaultInspector();

        // Reference to the target script
        LevelCreator builder = (LevelCreator)target;

        // Add a button
        if (GUILayout.Button("Create Level"))
        {
            builder.OnTryCreate();
        }
    }
}