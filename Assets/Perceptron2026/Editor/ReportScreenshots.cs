using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Takes the Unity Editor screenshots used in the report (menu: Perceptron / Take Report Screenshots,
/// or create Temp/report_capture.request while the editor is open). Walks through the steps below,
/// enters Play mode where needed and saves the main editor window as PNG (PrintWindow). Scenes are never saved.
/// State survives the domain reload of entering Play mode through SessionState.
/// </summary>
[InitializeOnLoad]
public static class ReportScreenshots
{
    private const string RequestFile = "Temp/report_capture.request";
    private const string DoneFile = "Temp/report_capture.done";
    private const string OutDir = "docs/week08/images/editor";
    private const string StepKey = "PcShots.Step", TimeKey = "PcShots.T0", EnteredKey = "PcShots.Entered";

    // step: what to show                                   needs Play   wait (s)
    //  1 LogicGates: Perceptron_OR training set              no          3
    //  2 LogicGates Play: Console with errors and tests      yes         2 (game time)
    //  3 stop                                                no          2
    //  4 SimpleGrapher Play: graph + 14 elements             yes         2 (game time)
    //  5 stop                                                no          2
    //  6 DodgeBall: Character, Animator + Rigidbody          no          3
    //  7 Animator window: Idle -> Crouch                     no          3
    //  8 DodgeBall Play: automatic throws, a duck            yes         see Tick
    //  9 stop                                                no          2
    private static readonly float[] Wait = { 0f, 3f, 2f, 2f, 2f, 2f, 3f, 3f, 0f, 2f };
    private static readonly int[] PlayDodgeKeys = { 4, 2, 1, 3, 1, 4, 1 };

    static ReportScreenshots()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("Perceptron/Take Report Screenshots")]
    public static void Begin()
    {
        Directory.CreateDirectory(OutDir);
        if (File.Exists(DoneFile)) File.Delete(DoneFile);
        SessionState.SetInt(StepKey, 1);
        SessionState.SetBool(EnteredKey, false);
    }

    private static double Now => EditorApplication.timeSinceStartup;

    private static void Tick()
    {
        if (File.Exists(RequestFile))
        {
            File.Delete(RequestFile);
            Begin();
        }
        int step = SessionState.GetInt(StepKey, 0);
        if (step == 0) return;
        if (step == 1 && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        if (!SessionState.GetBool(EnteredKey, false))
        {
            SessionState.SetBool(EnteredKey, true);
            SessionState.SetFloat(TimeKey, (float)Now);
            try { Enter(step); } catch (Exception e) { Fail(e); }
            return;
        }

        bool wantsPlay = step == 2 || step == 4 || step == 8;
        bool wantsStop = step == 3 || step == 5 || step == 9;
        if ((wantsPlay && !EditorApplication.isPlaying) || (wantsStop && EditorApplication.isPlayingOrWillChangePlaymode)
            || EditorApplication.isCompiling)
        {
            SessionState.SetFloat(TimeKey, (float)Now);
            return;
        }
        InternalEditorUtility.RepaintAllViews(); // an editor behind other windows does not repaint on its own
        if (EditorApplication.isPlaying)
        {
            // entering Play mode takes a few seconds: count game time, not editor time
            if ((step == 2 || step == 4) && Time.timeSinceLevelLoad < 1.5f) { SessionState.SetFloat(TimeKey, (float)Now); return; }
            if (step == 8 && !DodgeThrows()) return;
        }
        if (Now - SessionState.GetFloat(TimeKey, 0f) < Wait[step]) return;

        try { Leave(step); } catch (Exception e) { Fail(e); return; }
        int next = step + 1 < Wait.Length ? step + 1 : 0;
        SessionState.SetInt(StepKey, next);
        SessionState.SetBool(EnteredKey, false);
        if (next == 0)
        {
            File.WriteAllText(DoneFile, "ok");
            Debug.Log("Perceptron: report screenshots saved to " + OutDir);
        }
    }

    /// <summary>Throws one object every 1.2 s of game time; ready to capture just after the 7th throw (a red sphere).</summary>
    private static bool DodgeThrows()
    {
        float t = Time.timeSinceLevelLoad - 1f;
        int due = t < 0 ? 0 : Mathf.Min(PlayDodgeKeys.Length, 1 + (int)(t / 1.2f));
        int done = SessionState.GetInt("PcShots.Throws", 0);
        if (done < due)
        {
            var thrower = UnityEngine.Object.FindAnyObjectByType<DodgeBall.Throw>();
            thrower.ThrowObject(PlayDodgeKeys[done]);
            SessionState.SetInt("PcShots.Throws", done + 1);
            SessionState.SetFloat("PcShots.LastThrow", Time.timeSinceLevelLoad);
            return false;
        }
        // capture 0.2 s after the last throw: the character is crouching and the ball is in the air
        return done == PlayDodgeKeys.Length && Time.timeSinceLevelLoad - SessionState.GetFloat("PcShots.LastThrow", 0f) > 0.2f;
    }

    private static void Enter(int step)
    {
        switch (step)
        {
            case 1:
                EditorApplication.ExecuteMenuItem("Window/Layouts/Default");
                ClearConsole();
                Open(PerceptronSceneBuilder.LogicScenePath);
                Select("Perceptron_OR", "ts");
                break;
            case 2:
                ClearConsole();
                EditorApplication.ExecuteMenuItem("Window/General/Console");
                Select("Perceptron_OR", "ts");
                EditorApplication.isPlaying = true;
                break;
            case 3:
            case 5:
                EditorApplication.isPlaying = false;
                break;
            case 4:
                Open(PerceptronSceneBuilder.GrapherScenePath);
                Select("Perceptron", "ts");
                FocusGameView();
                EditorApplication.isPlaying = true;
                break;
            case 6:
                Open(PerceptronSceneBuilder.DodgeScenePath);
                EditorApplication.ExecuteMenuItem("Window/General/Project");
                Select("Character", null);
                FocusSceneOn(new Vector3(0f, 1f, 2.2f));
                break;
            case 7:
                EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
                break;
            case 8:
                SessionState.SetInt("PcShots.Throws", 0);
                ClearConsole();
                EditorApplication.ExecuteMenuItem("Window/General/Console");
                Select("Perceptron", null);
                FocusGameView();
                EditorApplication.isPlaying = true;
                break;
            case 9:
                EditorApplication.isPlaying = false;
                break;
        }
    }

    private static void Leave(int step)
    {
        switch (step)
        {
            case 1: Capture("01_logic_or_training_set.png"); break;
            case 2: Capture("02_logic_play_console.png"); break;
            case 4: Capture("03_grapher_play.png"); break;
            case 6: Capture("04_dodgeball_character.png"); break;
            case 7: Capture("05_animator_crouch.png"); break;
            case 8: Capture("06_dodgeball_play.png"); break;
            case 9:
                Open(PerceptronSceneBuilder.DodgeScenePath);
                FocusGameView();
                break;
        }
    }

    private static void Open(string path) => EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

    private static void Select(string name, string expandProperty)
    {
        GameObject go = GameObject.Find(name);
        Selection.activeGameObject = go;
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        if (go == null || expandProperty == null) return;
        foreach (var c in go.GetComponents<MonoBehaviour>())
        {
            var p = new SerializedObject(c).FindProperty(expandProperty);
            if (p != null) p.isExpanded = true; // editor UI state only
        }
        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }

    private static void FocusSceneOn(Vector3 target)
    {
        SceneView view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        view.Focus();
        view.LookAtDirect(target, Quaternion.Euler(15f, 200f, 0f), 2.2f);
        view.Repaint();
    }

    private static void FocusGameView()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType != null) EditorWindow.GetWindow(gameViewType, false, null, true);
    }

    private static void ClearConsole()
    {
        Type.GetType("UnityEditor.LogEntries,UnityEditor")?.GetMethod("Clear")?.Invoke(null, null);
    }

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public int biSize, biWidth, biHeight; public short biPlanes, biBitCount;
        public int biCompression, biSizeImage, biXPelsPerMeter, biYPelsPerMeter, biClrUsed, biClrImportant;
    }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr bmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER info, uint usage);

    /// <summary>
    /// Copies the main editor window with PrintWindow, which also works while the window is behind other
    /// windows, so the screenshots never take the screen away from whoever is using the computer.
    /// </summary>
    private static void Capture(string file)
    {
        IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out RECT r))
            throw new InvalidOperationException("Editor window not found; screenshot not taken: " + file);
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        IntPtr screen = GetDC(IntPtr.Zero), mem = CreateCompatibleDC(screen), bmp = CreateCompatibleBitmap(screen, w, h);
        IntPtr old = SelectObject(mem, bmp);
        try
        {
            if (!PrintWindow(hwnd, mem, 2)) // PW_RENDERFULLCONTENT
                throw new InvalidOperationException("PrintWindow failed: " + file);
            var info = new BITMAPINFOHEADER { biSize = Marshal.SizeOf<BITMAPINFOHEADER>(), biWidth = w, biHeight = h, biPlanes = 1, biBitCount = 32 };
            var bgra = new byte[w * h * 4];
            SelectObject(mem, old);
            GetDIBits(mem, bmp, 0, (uint)h, bgra, ref info, 0); // bottom-up rows, as Texture2D expects
            var rgba = new Color32[w * h];
            for (int i = 0; i < rgba.Length; i++)
                rgba[i] = new Color32(bgra[i * 4 + 2], bgra[i * 4 + 1], bgra[i * 4], 255);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(rgba);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            DeleteObject(bmp);
            DeleteDC(mem);
            ReleaseDC(IntPtr.Zero, screen);
        }
        Debug.Log("Perceptron: captured " + file);
    }

    private static void Fail(Exception e)
    {
        SessionState.SetInt(StepKey, 0);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        File.WriteAllText(DoneFile, "error: " + e);
        Debug.LogException(e);
    }
}
