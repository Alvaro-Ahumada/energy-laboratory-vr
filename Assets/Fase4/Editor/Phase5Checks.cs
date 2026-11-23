using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Runs the release checks with a Game view and builds the enabled laboratory scene.</summary>
public static class Phase5Checks
{
    /// <summary>Keep the simulated player loop active during unattended Editor checks.</summary>
    public static void Baseline()
    {
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.isPaused = false;
        Revision401Tests.Run();
    }

    /// <summary>Drive buttons and motion through the simulated controller input readers.</summary>
    public static void Controls()
    {
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.isPaused = false;
        Revision401Locomotion.Run();
    }

    /// <summary>Build the Android player and preserve the report separately from the source repository.</summary>
    public static void Build()
    {
        string directory = Environment.GetEnvironmentVariable("ENERGY_BUILD_DIR");
        if (string.IsNullOrEmpty(directory)) directory = Path.GetFullPath("Builds");
        Directory.CreateDirectory(directory);
        var options = new BuildPlayerOptions {
            scenes = new[] { Fase2Audit.ScenePath },
            locationPathName = Path.Combine(directory, "EnergyLab_5.0.0_500.apk"),
            target = BuildTarget.Android, options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var s = report.summary;
        File.WriteAllText(Path.Combine(directory, "BuildReport.txt"),
            $"Unity {Application.unityVersion}\nVersion {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})\nScene {Fase2Audit.ScenePath}\nResult {s.result}\nErrors {s.totalErrors}\nWarnings {s.totalWarnings}\nDuration {s.totalTime}\nBuild GUID {s.guid}");
        if (s.result != BuildResult.Succeeded) throw new InvalidOperationException(s.result.ToString());
    }
}
