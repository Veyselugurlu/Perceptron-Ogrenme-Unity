using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace DodgeBall
{
    /// <summary>
    /// Command-line switches for a standalone build; without -autoThrow the component does nothing.
    ///   -autoThrow             throw automatically: a random key 1-4 every -interval seconds (default 1)
    ///   -seed 7                seeds the initial weights (UnityEngine.Random) and the throw sequence
    ///   -throws 40             number of throws, then quit
    ///   -experiment &lt;csv&gt;     append one row per throw: decision, whether it was right, whether it hit
    ///   -recordFrames &lt;dir&gt;   save every -recordEvery-th frame as PNG at -fps frames of game time per second
    /// Game time advances by a fixed step per frame (Time.captureDeltaTime), so a run is reproducible.
    /// </summary>
    [DefaultExecutionOrder(-100)] // seed Random before Perceptron.Start draws the initial weights
    public class DodgeExperiment : MonoBehaviour
    {
        [SerializeField] private Throw thrower;
        [SerializeField] private Perceptron perceptron;
        [SerializeField] private DodgeStats stats;

        private int seed = 1, throws = 40;
        private float interval = 1f;
        private string csvPath;
        private System.Random sequence;
        private readonly Dictionary<int, int> ballToThrow = new Dictionary<int, int>();
        private readonly List<Row> rows = new List<Row>();

        private class Row
        {
            public int key; public double shape, colour, desired, decision; public bool hit;
            public double w1, w2, bias;
        }

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-autoThrow") < 0)
            {
                enabled = false;
                return;
            }
            if (int.TryParse(Value(args, "-seed"), out int s)) seed = s;
            if (int.TryParse(Value(args, "-throws"), out int n)) throws = Mathf.Max(1, n);
            if (float.TryParse(Value(args, "-interval"), NumberStyles.Float, CultureInfo.InvariantCulture, out float i)) interval = i;
            csvPath = Value(args, "-experiment");
            UnityEngine.Random.InitState(seed);
            sequence = new System.Random(seed); // separate stream: the throw order does not depend on physics or weights

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            string dir = Value(args, "-recordFrames");
            if (!string.IsNullOrEmpty(dir))
            {
                float fps = float.TryParse(Value(args, "-fps"), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 20f;
                Time.captureDeltaTime = 1f / fps;
                int every = int.TryParse(Value(args, "-recordEvery"), out int e) ? e : 1;
                StartCoroutine(Record(dir, every));
            }
            else
            {
                Time.captureDeltaTime = Time.fixedDeltaTime; // one physics step per frame
            }
        }

        private void Start()
        {
            perceptron.Reacted += OnReacted;
            stats.Hit += OnHit;
            StartCoroutine(ThrowSequence());
        }

        private void OnReacted(double shape, double colour, double desired, double decision)
        {
            rows.Add(new Row
            {
                shape = shape, colour = colour, desired = desired, decision = decision,
                w1 = perceptron.Weight1, w2 = perceptron.Weight2, bias = perceptron.Bias,
            });
        }

        private void OnHit(GameObject ball, bool redSphere)
        {
            if (ballToThrow.TryGetValue(ball.GetInstanceID(), out int index)) rows[index].hit = true;
        }

        private IEnumerator ThrowSequence()
        {
            yield return new WaitForSeconds(0.5f);
            for (int t = 0; t < throws; t++)
            {
                int key = sequence.Next(1, 5);
                GameObject ball = thrower.ThrowObject(key);
                rows[rows.Count - 1].key = key;
                ballToThrow[ball.GetInstanceID()] = rows.Count - 1;
                yield return new WaitForSeconds(interval);
            }
            yield return new WaitForSeconds(1f); // let the last object arrive
            if (!string.IsNullOrEmpty(csvPath)) WriteCsv();
            Debug.Log($"DodgeExperiment: seed {seed}, {throws} throws, wrong decisions {stats.WrongDecisions}, red hits {stats.RedHits}");
            Application.Quit();
        }

        private void WriteCsv()
        {
            var sb = new StringBuilder();
            if (!File.Exists(csvPath)) sb.Append("seed,throw,key,shape,colour,desired,decision,correct,hit,w1,w2,bias\n");
            for (int t = 0; t < rows.Count; t++)
            {
                Row r = rows[t];
                sb.Append(string.Join(",", seed, t + 1, r.key, r.shape, r.colour, r.desired, r.decision,
                    r.decision == r.desired ? 1 : 0, r.hit ? 1 : 0, F(r.w1), F(r.w2), F(r.bias))).Append('\n');
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(csvPath)));
            File.AppendAllText(csvPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static IEnumerator Record(string dir, int every)
        {
            Directory.CreateDirectory(dir);
            for (int saved = 0, frame = 0; ; frame++)
            {
                yield return new WaitForEndOfFrame();
                if (frame % every != 0) continue;
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(dir, $"frame_{saved++:0000}.png"), shot.EncodeToPNG());
                Destroy(shot);
            }
        }

        private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Value(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
