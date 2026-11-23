using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Inspects scene configuration for Fase2Audit.</summary>
public static class Fase2Audit
{
    public const string ScenePath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoScene.unity";
    public static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Fase 2 (Construcción del entorno de realidad virtual)/Desarrollo/Evidencias"));
    /// <summary>Collects the initial scene and configuration evidence.</summary>
    public static void Baseline()
    {
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var lines = new System.Collections.Generic.List<string>();
        lines.Add("UTC=" + DateTime.UtcNow.ToString("o"));
        lines.Add("Unity=" + Application.unityVersion);
        lines.Add("BuildTarget=" + EditorUserBuildSettings.activeBuildTarget);
        lines.Add("Scene=" + scene.path);
        int missing = 0;
        foreach (var root in scene.GetRootGameObjects()) {
            lines.Add("ROOT " + root.name + " active=" + root.activeSelf + " position=" + root.transform.position);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) {
                int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                missing += n;
                if (n > 0) lines.Add("MISSING " + t.name + ":" + n);
            }
        }
        lines.Add("MissingScripts=" + missing);
        lines.Add("Cameras=" + UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
        File.WriteAllLines(Path.Combine(Output, "Unity_baseline.txt"), lines);
        Debug.Log("FASE2_BASELINE_OK missing=" + missing);
    }
}
