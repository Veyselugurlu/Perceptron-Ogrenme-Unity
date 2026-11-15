using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Trains Grapher.Perceptron (the SimpleGrapher scene's script) on the food/weapon data with 100 seeds
/// and writes docs/week08/data/grapher_runs.csv:
///   first epoch after which all 14 objects are classified correctly (training stops changing there),
///   weights after Train(200) as in the scene, the decision line, and the two test objects' classes.
/// Two training sets are compared: the 14 elements of the package scene and the table printed in the
/// lecture figure (croissant (0.8, 1) and axe (0, 0.7)).
/// Menu: Perceptron / Run SimpleGrapher Experiments, batch: -executeMethod GrapherExperiments.RunBatch
/// </summary>
public static class GrapherExperiments
{
    private const string OutPath = "docs/week08/data/grapher_runs.csv";
    private const int Seeds = 100;
    private const int SceneEpochs = 200; // Start() calls Train(200)

    [MenuItem("Perceptron/Run SimpleGrapher Experiments")]
    public static void Run()
    {
        var csv = new StringBuilder("set,seed,first_all_correct_epoch,correct_after_200,w1,w2,bias,slope,intercept,test_03_09,test_08_01\n");
        var go = new GameObject("ExperimentPerceptron") { hideFlags = HideFlags.HideAndDontSave };
        bool logWasEnabled = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false; // Train logs every update
        try
        {
            foreach (var (name, rows) in new[] { ("scene", GrapherData.SceneSet), ("figure", GrapherData.FigureSet) })
            {
                var p = go.AddComponent<Grapher.Perceptron>();
                p.ts = GrapherData.ToTrainingSet(rows);
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    // Train re-initialises the weights from Random, so the same seed replays the same run:
                    // training n epochs gives the state after epoch n of a longer run.
                    int first = -1;
                    for (int n = 1; n <= SceneEpochs && first < 0; n++)
                    {
                        Random.InitState(seed);
                        p.TrainEpochs(n);
                        if (Correct(p, rows) == rows.Length) first = n;
                    }
                    Random.InitState(seed);
                    p.TrainEpochs(SceneEpochs);
                    double w1 = p.Weight1, w2 = p.Weight2, b = p.Bias;
                    // line w1*x + w2*y + b = 0  ->  y = -(w1/w2) x - b/w2  (the scene passes the same two numbers to DrawRay)
                    csv.Append(string.Join(",", name, seed, first < 0 ? "" : first.ToString(), Correct(p, rows),
                        F(w1), F(w2), F(b), F(-w1 / w2), F(-b / w2), p.Output(0.3, 0.9), p.Output(0.8, 0.1))).Append('\n');
                }
                Object.DestroyImmediate(p);
            }
        }
        finally
        {
            Debug.unityLogger.logEnabled = logWasEnabled;
            Object.DestroyImmediate(go);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, csv.ToString(), new UTF8Encoding(false));
        Debug.Log("Perceptron: SimpleGrapher experiments written to " + OutPath);
    }

    private static int Correct(Grapher.Perceptron p, double[][] rows)
    {
        int ok = 0;
        foreach (var r in rows) if (p.Output(r[0], r[1]) == r[2]) ok++;
        return ok;
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
