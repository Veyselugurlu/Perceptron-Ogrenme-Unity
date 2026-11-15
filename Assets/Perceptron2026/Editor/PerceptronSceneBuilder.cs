using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the week's scenes from code (menu: Perceptron). No external assets are used.
///   LogicGates.unity   three perceptrons with the same script, trained on OR, AND and XOR data
/// The other scenes are in the PerceptronSceneBuilder.*.cs files.
/// </summary>
public static partial class PerceptronSceneBuilder
{
    public const string Root = "Assets/Perceptron2026";
    public const string LogicScenePath = Root + "/LogicGates/LogicGates.unity";

    [MenuItem("Perceptron/1. Logic Gates Scene")]
    public static void BuildLogicGates()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        AddGate("Perceptron_OR", LogicGateData.Or);
        AddGate("Perceptron_AND", LogicGateData.And);
        AddGate("Perceptron_XOR", LogicGateData.Xor);
        Save(scene, LogicScenePath);
    }

    private static void AddGate(string name, double[][] rows)
    {
        var go = new GameObject(name);
        var p = go.AddComponent<LogicGates.Perceptron>();
        p.ts = LogicGateData.ToTrainingSet(rows);
    }

    private static void Save(UnityEngine.SceneManagement.Scene scene, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Perceptron: scene saved -> " + path);
    }

    /// <summary>Batch entry point (-executeMethod PerceptronSceneBuilder.BuildAllBatch).</summary>
    public static void BuildAllBatch()
    {
        int code = 0;
        try { BuildAll(); }
        catch (System.Exception e) { Debug.LogException(e); code = 1; }
        EditorApplication.Exit(code);
    }

    [MenuItem("Perceptron/Build All Scenes")]
    public static void BuildAll()
    {
        BuildLogicGates();
        BuildGrapher();
        BuildDodgeBall();
        RegisterScenes();
    }

    private static void RegisterScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(LogicScenePath, true),
            new EditorBuildSettingsScene(GrapherScenePath, true),
            new EditorBuildSettingsScene(DodgeScenePath, true),
        };
        if (File.Exists("Assets/Scenes/SampleScene.unity"))
        {
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
            if (Directory.GetFiles("Assets/Scenes").Length == 0) AssetDatabase.DeleteAsset("Assets/Scenes");
        }
    }
}

/// <summary>Truth tables used as training sets: { input1, input2, output }.</summary>
public static class LogicGateData
{
    public static readonly double[][] Or = { new double[] { 0, 0, 0 }, new double[] { 0, 1, 1 }, new double[] { 1, 0, 1 }, new double[] { 1, 1, 1 } };
    public static readonly double[][] And = { new double[] { 0, 0, 0 }, new double[] { 0, 1, 0 }, new double[] { 1, 0, 0 }, new double[] { 1, 1, 1 } };
    public static readonly double[][] Xor = { new double[] { 0, 0, 0 }, new double[] { 0, 1, 1 }, new double[] { 1, 0, 1 }, new double[] { 1, 1, 0 } };

    public static LogicGates.TrainingSet[] ToTrainingSet(double[][] rows)
    {
        var ts = new LogicGates.TrainingSet[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            ts[i] = new LogicGates.TrainingSet { input = new[] { rows[i][0], rows[i][1] }, output = rows[i][2] };
        return ts;
    }
}
