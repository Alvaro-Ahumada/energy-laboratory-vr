using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.OpenXR;
/// <summary>Inspects scene configuration for Fase4Audit.</summary>
public static class Fase4Audit {
 public static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,"../../F4Trabajo/Evidencias"));
 /// <summary>Collects the initial scene and configuration evidence.</summary>
 public static void Baseline() {
 Directory.CreateDirectory(Output);var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);var b=new StringBuilder();
 b.AppendLine("UTC="+DateTime.UtcNow.ToString("o"));b.AppendLine("Unity="+Application.unityVersion);b.AppendLine("Target="+EditorUserBuildSettings.activeBuildTarget);b.AppendLine("Scene="+scene.path);
 foreach(var root in scene.GetRootGameObjects()) b.AppendLine("ROOT "+root.name+" active="+root.activeSelf);
 int missing=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));b.AppendLine("MissingScripts="+missing);
 foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(m) b.AppendLine("COMP "+m.GetType().FullName+" enabled="+m.enabled+" active="+m.gameObject.activeInHierarchy+" object="+m.name);
 foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) b.AppendLine("LIGHT "+l.name+" type="+l.type+" bake="+l.lightmapBakeType+" shadows="+l.shadows);
 foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) b.AppendLine("RENDER "+r.name+" static="+GameObjectUtility.GetStaticEditorFlags(r.gameObject)+" lm="+r.lightmapIndex);
 var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);foreach(var f in xr.GetFeatures()) b.AppendLine("XR "+f.GetType().Name+" enabled="+f.enabled);
 b.AppendLine("Lightmaps="+LightmapSettings.lightmaps.Length);b.AppendLine("URP="+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
 File.WriteAllText(Path.Combine(Output,"Baseline_inicial.txt"),b.ToString());Debug.Log("F4_BASELINE_OK");
 }
}
