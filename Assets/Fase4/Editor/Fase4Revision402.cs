using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Builds the archived laboratory revision for Fase4Revision402.</summary>
public static class Fase4Revision402
{
    /// <summary>Builds the corresponding project revision in Unity.</summary>
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        var architecture = GameObject.Find("Arquitectura").transform;
        var stretched = architecture.Cast<Transform>().SingleOrDefault(t => t.name == "ModuloMuro" && Mathf.Approximately(t.localScale.x, 6));
        if (stretched != null)
        {
            var center = stretched.localPosition;
            for (int i = 0; i < 6; i++)
            {
                var module = i == 0 ? stretched : UnityEngine.Object.Instantiate(stretched, architecture);
                module.name = "Muro_Entrada_" + (i + 1);
                module.localScale = Vector3.one;
                module.localPosition = center + Vector3.right * (-5 + 2 * i);
            }
        }
        var modules = architecture.Cast<Transform>().Where(t => t.name.StartsWith("Muro_Entrada_")).OrderBy(t => t.localPosition.x).ToArray();
        if (modules.Length != 6 || modules.Where((t,i) => t.localScale != Vector3.one || Mathf.Abs(t.localPosition.x - (-5+2*i))>.01f).Any())
            throw new Exception("La pared modular no cumple las dimensiones previstas.");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!Lightmapping.Bake()) throw new Exception("Fallo de iluminación");
        EditorSceneManager.SaveScene(scene);
        PlayerSettings.bundleVersion = "4.0.2";
        PlayerSettings.Android.bundleVersionCode = 402;
        AssetDatabase.SaveAssets();
        string apk = Path.GetFullPath(Path.Combine(Application.dataPath,"../../F4Trabajo/Revision402/APK/Laboratorio_4.0.2_v402.apk"));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Fase2Audit.ScenePath},locationPathName=apk,target=BuildTarget.Android,options=BuildOptions.None});
        if(report.summary.result != BuildResult.Succeeded) throw new Exception("Fallo de compilación");
        Debug.Log("REV402_OK modules=6 errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
    }
}
