using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
///   DodgeBall/DodgeBall.unity   a character that learns to duck only for red spheres
/// The lecture package used Ethan from the old Standard Assets (about 30 MB of models and textures).
/// Here the character is built from primitives and gets an Animator Controller with the same
/// "Crouch" trigger, so Perceptron.SendInput works unchanged.
/// </summary>
public static partial class PerceptronSceneBuilder
{
    public const string DodgeScenePath = Root + "/DodgeBall/DodgeBall.unity";
    private const string DodgeDir = Root + "/DodgeBall";

    // character stands 2.2 m in front of the camera: a ball thrown at 10 m/s from 1.6 m reaches it
    // at about 1.35 m, above a crouching character (0.9 m) and into a standing one (1.8 m)
    private static readonly Vector3 CharacterPosition = new Vector3(0f, 0f, 2.2f);

    [MenuItem("Perceptron/3. DodgeBall Scene")]
    public static void BuildDodgeBall()
    {
        Directory.CreateDirectory(DodgeDir + "/Materials");
        Directory.CreateDirectory(DodgeDir + "/Prefabs");
        Directory.CreateDirectory(DodgeDir + "/Animation");
        Material red = Mat("red", new Color(0.85f, 0.15f, 0.15f));
        Material green = Mat("green", new Color(0.15f, 0.7f, 0.25f));
        Material skin = Mat("character", new Color(0.75f, 0.75f, 0.8f));
        Material dark = Mat("visor", new Color(0.1f, 0.1f, 0.12f));
        Material floor = Mat("floor", new Color(0.35f, 0.38f, 0.42f));
        GameObject sphere = Projectile("Sphere", PrimitiveType.Sphere, red);
        GameObject cube = Projectile("Cube", PrimitiveType.Cube, red);
        AnimatorController controller = CharacterController();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera cam = Camera.main;
        cam.transform.SetPositionAndRotation(new Vector3(0.25f, 1.6f, 0f), Quaternion.Euler(15f, -6f, 0f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.18f, 0.22f);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Plane";
        plane.GetComponent<Renderer>().sharedMaterial = floor;

        GameObject character = Character(controller, skin, dark);

        var perceptronGo = new GameObject("Perceptron");
        var perceptron = perceptronGo.AddComponent<DodgeBall.Perceptron>();
        perceptron.npc = character;
        var thrower = perceptronGo.AddComponent<DodgeBall.Throw>();
        thrower.spherePrefab = sphere;
        thrower.cubePrefab = cube;
        thrower.red = red;
        thrower.green = green;

        var stats = new SerializedObject(character.AddComponent<DodgeBall.DodgeStats>());
        stats.FindProperty("perceptron").objectReferenceValue = perceptron;
        stats.FindProperty("red").objectReferenceValue = red;
        stats.ApplyModifiedPropertiesWithoutUndo();

        var driver = new SerializedObject(perceptronGo.AddComponent<DodgeBall.DodgeExperiment>()); // idle without -autoThrow
        driver.FindProperty("thrower").objectReferenceValue = thrower;
        driver.FindProperty("perceptron").objectReferenceValue = perceptron;
        driver.FindProperty("stats").objectReferenceValue = stats.targetObject;
        driver.ApplyModifiedPropertiesWithoutUndo();

        Save(scene, DodgeScenePath);
    }

    [MenuItem("Perceptron/Build DodgeBall Player")]
    public static bool BuildDodgePlayer()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { DodgeScenePath },
            locationPathName = "Builds/DodgeBall/DodgeBall.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log("Perceptron: DodgeBall build " + report.summary.result);
        return report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
    }

    /// <summary>Batch entry point: all scenes, then the DodgeBall player (-executeMethod PerceptronSceneBuilder.BuildAllAndPlayerBatch).</summary>
    public static void BuildAllAndPlayerBatch()
    {
        int code = 0;
        try { BuildAll(); if (!BuildDodgePlayer()) code = 1; }
        catch (System.Exception e) { Debug.LogException(e); code = 1; }
        EditorApplication.Exit(code);
    }

    private static Material Mat(string name, Color color)
    {
        string path = $"{DodgeDir}/Materials/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", color);
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    private static GameObject Projectile(string name, PrimitiveType type, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localScale = Vector3.one * 0.3f;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f; // AddForce(0, 0, 500) for one physics step (0.02 s) -> 10 m/s
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        go.AddComponent<DodgeBall.Lifetime>();
        string path = $"{DodgeDir}/Prefabs/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static GameObject Character(AnimatorController controller, Material skin, Material dark)
    {
        var root = new GameObject("Character");
        root.transform.SetPositionAndRotation(CharacterPosition, Quaternion.Euler(0f, 180f, 0f)); // faces the camera
        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        var rb = root.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll; // as in the package scene: it never gets knocked over
        rb.isKinematic = true;

        var pivot = new GameObject("Pivot").transform; // the crouch animation scales this in y
        pivot.SetParent(root.transform, false);
        Part(PrimitiveType.Capsule, "Body", pivot, new Vector3(0f, 0.75f, 0f), new Vector3(0.6f, 0.75f, 0.6f), skin);
        Part(PrimitiveType.Sphere, "Head", pivot, new Vector3(0f, 1.62f, 0f), Vector3.one * 0.35f, skin);
        GameObject visor = Part(PrimitiveType.Cube, "Visor", pivot, new Vector3(0f, 1.65f, 0.16f), new Vector3(0.24f, 0.07f, 0.06f), dark);
        Object.DestroyImmediate(visor.GetComponent<Collider>());
        return root;
    }

    private static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>Idle and Crouch states; the "Crouch" trigger starts a 0.95 s crouch that returns to Idle.</summary>
    private static AnimatorController CharacterController()
    {
        AnimationClip idle = Clip("Idle", new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        AnimationClip crouch = Clip("Crouch", new Keyframe(0f, 1f), new Keyframe(0.08f, 0.5f),
                                    new Keyframe(0.75f, 0.5f), new Keyframe(0.95f, 1f));
        string path = $"{DodgeDir}/Animation/Character.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Crouch", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;
        AnimatorState crouchState = sm.AddState("Crouch");
        crouchState.motion = crouch;
        AnimatorStateTransition down = idleState.AddTransition(crouchState);
        down.hasExitTime = false;
        down.duration = 0f;
        down.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
        AnimatorStateTransition up = crouchState.AddTransition(idleState);
        up.hasExitTime = true;
        up.exitTime = 1f;
        up.duration = 0f;
        return controller;
    }

    private static AnimationClip Clip(string name, params Keyframe[] scaleY)
    {
        string path = $"{DodgeDir}/Animation/{name}.anim";
        var clip = new AnimationClip { name = name };
        var curve = new AnimationCurve(scaleY);
        for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        clip.SetCurve("Pivot", typeof(Transform), "m_LocalScale.y", curve);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
