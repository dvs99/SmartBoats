using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor class to create buttons for the GenerationManager.
/// </summary>
[CustomEditor(typeof(GenerationManager)), CanEditMultipleObjects]
public class GenerationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        if (GUILayout.Button("Clear Scores"))
        {
            (target as GenerationManager)?.clearScoresFile();
        }
        if (GUILayout.Button("Generate Boxes"))
        {
            (target as GenerationManager)?.GenerateBoxes();
        }
        if (GUILayout.Button("Generate Boats"))
        {
            (target as GenerationManager)?.GenerateBoats();
        }
        if (GUILayout.Button("Start Simulation"))
        {
            (target as GenerationManager)?.StartSimulation();
        }
        if (GUILayout.Button("Continue Simulation"))
        {
            (target as GenerationManager)?.ContinueSimulation();
        }
        if (GUILayout.Button("Stop Simulation"))
        {
            (target as GenerationManager)?.StopSimulation();
        }
        if (GUILayout.Button("Start Simulation From Stored Boats"))
        {
            (target as GenerationManager)?.StartSimulationStored();
        }
    }
}
