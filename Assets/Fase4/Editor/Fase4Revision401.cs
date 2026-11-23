using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>Builds the archived laboratory revision for Fase4Revision401.</summary>
public static class Fase4Revision401
{
    /// <summary>Builds the corresponding project revision in Unity.</summary>
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        var lab = UnityEngine.Object.FindFirstObjectByType<Fase3.LabSession>();
        var subtitle = lab.menu.GetComponentsInChildren<TMP_Text>(true).Single(t => t.text.Contains("FASE 3") || t.text.StartsWith("Teletransporte:"));
        subtitle.text = "Teletransporte: el salto es inmediato. Apunta cerca.";
        subtitle.enableAutoSizing = false;
        subtitle.fontSize = 18f;
        var wall = GameObject.Find("Arquitectura").GetComponentsInChildren<Transform>().Single(t => t.name == "ModuloMuro" && Mathf.Abs(t.localScale.x-6)<0.01f);
        bool rebake = wall.localScale.y < .99f || LightmapSettings.lightmaps.Length == 0;
        wall.localScale = new Vector3(6,1,1);
        var barrier = GameObject.Find("Limite_Entrada").GetComponent<BoxCollider>();
        barrier.center = new Vector3(0,1.5f,-3);
        barrier.size = new Vector3(12,3,.3f);
        var rig = scene.GetRootGameObjects().Single(g => g.name.Contains("XR Origin"));
        var rays = rig.GetComponentsInChildren<XRRayInteractor>(true).Where(r=>r.name.Contains("Teleport")).ToArray();
        if(rays.Length != 2) throw new Exception("Expected two teleport rays: " + rays.Length);
        foreach(var ray in rays) {ray.velocity=3f; PrefabUtility.RecordPrefabInstancePropertyModifications(ray);}
        foreach(var turn in rig.GetComponentsInChildren<SnapTurnProvider>(true)) {turn.turnAmount=22.5f; PrefabUtility.RecordPrefabInstancePropertyModifications(turn);}
        foreach(var move in rig.GetComponentsInChildren<ContinuousMoveProvider>(true)) {move.moveSpeed=.8f; PrefabUtility.RecordPrefabInstancePropertyModifications(move);}
        EditorSceneManager.MarkSceneDirty(scene);
        if(rebake && !Lightmapping.Bake()) throw new Exception("Lighting bake failed");
        EditorSceneManager.SaveScene(scene);
        PlayerSettings.bundleVersion="4.0.1";
        PlayerSettings.Android.bundleVersionCode=401;
        AssetDatabase.SaveAssets();
        var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../F4Trabajo/Revision401/APK/Fase4_Laboratorio_4.0.1_v401.apk"));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{Fase2Audit.ScenePath},locationPathName=path,target=BuildTarget.Android,options=BuildOptions.None });
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Android build failed");
        Debug.Log("F4_REV401_OK errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings);
    }
}
