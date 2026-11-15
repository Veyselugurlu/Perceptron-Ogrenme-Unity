using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Trains the LogicGates.Perceptron component (the same code as the scene) on OR, AND and XOR
/// with 100 seeds each and writes
///   docs/week08/data/logic_epochs.csv   total error of every epoch
///   docs/week08/data/logic_runs.csv     first error-free epoch, final weights and outputs per run
/// Menu: Perceptron / Run Logic Gate Experiments, batch: -executeMethod LogicGateExperiments.RunBatch
/// </summary>
public static class LogicGateExperiments
{
    private const string OutDir = "docs/week08/data";
    public const int Seeds = 100;

    private static readonly (string name, double[][] rows, int epochs)[] Gates =
    {
        ("OR", LogicGateData.Or, 50),
        ("AND", LogicGateData.And, 50),
        ("XOR", LogicGateData.Xor, 1000), // never error-free: try far more epochs than OR needs
    };

    [MenuItem("Perceptron/Run Logic Gate Experiments")]
    public static void Run()
    {
        var epochsCsv = new StringBuilder("gate,seed,epoch,total_error\n");
        var runsCsv = new StringBuilder("gate,seed,epochs_run,first_zero_error_epoch,w1,w2,bias,out00,out01,out10,out11,correct\n");
        var go = new GameObject("ExperimentPerceptron") { hideFlags = HideFlags.HideAndDontSave };
        bool logWasEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false; // Train logs every update; thousands of lines would slow the run down
        try
        {
            foreach (var gate in Gates)
            {
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    var p = go.AddComponent<LogicGates.Perceptron>();
                    p.ts = LogicGateData.ToTrainingSet(gate.rows);
                    int firstZero = -1;
                    string g = gate.name;
                    int s = seed;
                    p.EpochFinished += (epoch, error) =>
                    {
                        epochsCsv.Append($"{g},{s},{epoch},{F(error)}\n");
                        if (error == 0 && firstZero < 0) firstZero = epoch;
                    };
                    Random.InitState(seed); // same seed -> same initial weights for every gate
                    p.TrainEpochs(gate.epochs);

                    double[] outs = { p.Output(0, 0), p.Output(0, 1), p.Output(1, 0), p.Output(1, 1) };
                    int correct = 0;
                    for (int i = 0; i < 4; i++) if (outs[i] == gate.rows[i][2]) correct++;
                    runsCsv.Append(string.Join(",", g, seed, gate.epochs, firstZero < 0 ? "" : firstZero.ToString(),
                        F(p.Weight1), F(p.Weight2), F(p.Bias), outs[0], outs[1], outs[2], outs[3], correct)).Append('\n');
                    Object.DestroyImmediate(p);
                }
            }
        }
        finally
        {
            Debug.unityLogger.logEnabled = logWasEnabled;
            Object.DestroyImmediate(go);
        }
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(Path.Combine(OutDir, "logic_epochs.csv"), epochsCsv.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(OutDir, "logic_runs.csv"), runsCsv.ToString(), new UTF8Encoding(false));
        Debug.Log("Perceptron: logic gate experiments written to " + OutDir);
    }

    public static void RunBatch()
    {
        int code = 0;
        try { Run(); }
        catch (System.Exception e) { Debug.LogException(e); code = 1; }
        EditorApplication.Exit(code);
    }

    private static string F(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);
}
