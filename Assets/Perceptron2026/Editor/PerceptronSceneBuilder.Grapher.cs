using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///   SimpleGrapher/Perceptron.unity   Canvas + RawImage (SimpleGrapher) and the food/weapon perceptron
/// The package's blank.jpg is replaced by a black texture written here; SimpleGrapher draws into it,
/// so it must be readable and uncompressed.
/// </summary>
public static partial class PerceptronSceneBuilder
{
    public const string GrapherScenePath = Root + "/SimpleGrapher/Perceptron.unity";
    private const string BlankTexturePath = Root + "/SimpleGrapher/blank.png";

    [MenuItem("Perceptron/2. SimpleGrapher Scene (food / weapon)")]
    public static void BuildGrapher()
    {
        Texture2D blank = CreateBlankTexture();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.12f, 0.12f, 0.14f);

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1f;

        var imageGo = new GameObject("RawImage", typeof(RectTransform), typeof(RawImage));
        imageGo.transform.SetParent(canvasGo.transform, false);
        var rect = imageGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(680, 680);
        imageGo.GetComponent<RawImage>().texture = blank;
        var grapher = imageGo.AddComponent<Grapher.SimpleGrapher>(); // scale 500, offsets 100 (defaults)

        var p = new GameObject("Perceptron").AddComponent<Grapher.Perceptron>();
        p.ts = GrapherData.ToTrainingSet(GrapherData.SceneSet);
        p.sg = grapher;

        Save(scene, GrapherScenePath);
    }

    private static Texture2D CreateBlankTexture()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(BlankTexturePath));
        if (!File.Exists(BlankTexturePath))
        {
            var tex = new Texture2D(768, 768, TextureFormat.RGBA32, false);
            var pixels = new Color32[768 * 768];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(BlankTexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(BlankTexturePath);
        }
        var importer = (TextureImporter)AssetImporter.GetAtPath(BlankTexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;                       // SetPixel / GetPixels at runtime
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(BlankTexturePath);
    }
}

/// <summary>Food / weapon training set: { sharp, edible, food }.</summary>
public static class GrapherData
{
    /// <summary>The 14 elements stored in the package's Perceptron scene.</summary>
    public static readonly double[][] SceneSet =
    {
        new[] { 0.5, 1.0, 1.0 }, new[] { 0.0, 0.8, 1.0 }, new[] { 0.2, 1.0, 1.0 }, new[] { 0.1, 0.5, 1.0 },
        new[] { 0.3, 0.5, 1.0 }, new[] { 0.3, 0.8, 1.0 }, new[] { 0.5, 0.9, 1.0 }, new[] { 0.8, 1.0, 1.0 },
        new[] { 0.1, 0.0, 0.0 }, new[] { 0.5, 0.3, 0.0 }, new[] { 0.6, 0.3, 0.0 }, new[] { 0.6, 0.1, 0.0 },
        new[] { 0.7, 0.0, 0.0 }, new[] { 0.9, 0.0, 0.0 },
    };

    /// <summary>The same table as printed in the lecture's figure: croissant (0.8, 1) and axe (0, 0.7).</summary>
    public static readonly double[][] FigureSet =
    {
        new[] { 0.8, 1.0, 1.0 }, new[] { 0.0, 0.8, 1.0 }, new[] { 0.2, 1.0, 1.0 }, new[] { 0.1, 0.5, 1.0 },
        new[] { 0.3, 0.5, 1.0 }, new[] { 0.3, 0.8, 1.0 }, new[] { 0.5, 0.9, 1.0 }, new[] { 0.8, 1.0, 1.0 },
        new[] { 0.1, 0.0, 0.0 }, new[] { 0.5, 0.3, 0.0 }, new[] { 0.6, 0.3, 0.0 }, new[] { 0.6, 0.1, 0.0 },
        new[] { 0.0, 0.7, 0.0 }, new[] { 0.9, 0.0, 0.0 },
    };

    public static readonly string[] Names =
    {
        "Croissant", "Burger", "Strawberry", "Cupcake", "Fries", "Cutlet", "Carrot", "Pineapple",
        "Nunchuk", "Bow", "Stick", "Sword", "Axe", "Kunai",
    };

    public static Grapher.TrainingSet[] ToTrainingSet(double[][] rows)
    {
        var ts = new Grapher.TrainingSet[rows.Length];
        for (int i = 0; i < rows.Length; i++)
            ts[i] = new Grapher.TrainingSet { input = new[] { rows[i][0], rows[i][1] }, output = rows[i][2] };
        return ts;
    }
}
