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

        if (GUILayout.Button("Load From Level"))
        {
            Undo.RecordObject(builder, "Load level recipe");
            builder.OnLoadFromLevel();
        }

        if (GUILayout.Button("Create Level"))
        {
            if (builder._LevelData != null) Undo.RecordObject(builder._LevelData, "Create level");
            builder.OnTryCreate();
            AssetDatabase.SaveAssets();
        }
    }
}
