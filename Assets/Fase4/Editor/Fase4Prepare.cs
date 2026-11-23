using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
/// <summary>Prepares Android player settings for Fase4Prepare.</summary>
public static class Fase4Prepare {
 const string A="Assets/Fase4/";
 public static void ConfigureAndBake() {
  Directory.CreateDirectory(Fase4Audit.Output);var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
  var changes=new List<string>();
  // General Android candidate. The exact headset and installation remain pending.
  PlayerSettings.companyName="Alvaro Ahumada";PlayerSettings.productName="Laboratorio de Energia VR";
  PlayerSettings.bundleVersion="4.0.0";PlayerSettings.Android.bundleVersionCode=400;
  PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.alvaroahumada.laboratoriovr");
  PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
  PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
  PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;
  PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel34;
  PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
  PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
  EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
  EditorUserBuildSettings.development=false;EditorUserBuildSettings.connectProfiler=false;
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Fase2Audit.ScenePath,true)};
  var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
  foreach(var f in xr.GetFeatures()) {
   var n=f.GetType().Name;
   if(n.StartsWith("AndroidXR") || n=="MockRuntime") {if(f.enabled) changes.Add("Desactivar función ajena al destino: "+n);f.enabled=false;EditorUtility.SetDirty(f);}
  }
  // Preserve installed versions and the Meta Quest / Touch profiles.
  foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Fase2/Models"}).Select(AssetDatabase.GUIDToAssetPath)) {
   var im=AssetImporter.GetAtPath(path) as ModelImporter;if(im==null)continue;
   im.generateSecondaryUV=true;im.SaveAndReimport();changes.Add("UV2 de lightmap: "+path);
  }
  foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Fase2/Textures"}).Select(AssetDatabase.GUIDToAssetPath)) {
   var im=AssetImporter.GetAtPath(path) as TextureImporter;if(im==null)continue;
   var a=im.GetPlatformTextureSettings("Android");a.overridden=true;a.maxTextureSize=1024;a.format=TextureImporterFormat.ASTC_6x6;im.SetPlatformTextureSettings(a);im.SaveAndReimport();changes.Add("Android ASTC 6x6 máximo 1024: "+path);
  }
  var root=GameObject.Find("Fase2_Laboratorio").transform;int statics=0;
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>()) {
   if(r.GetComponentInParent<Fase3.CellInteraction>() || r.GetComponent<TMPro.TMP_Text>())continue;
   if(r.transform.IsChildOf(root.Find("Arquitectura")) || r.transform.root==root && (r.name.Contains("Pedestal")||r.name.Contains("Estacion")) || r.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Pedestal_")||t.name=="Estacion_Activacion")) {
    GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
    r.receiveGI=ReceiveGI.Lightmaps;statics++;
   }
  }
  var settings=AssetDatabase.LoadAssetAtPath<LightingSettings>(A+"Lighting/Fase4_Lighting.lighting");
  if(!settings){Directory.CreateDirectory(A+"Lighting");AssetDatabase.Refresh();settings=new LightingSettings();AssetDatabase.CreateAsset(settings,A+"Lighting/Fase4_Lighting.lighting");}
  settings.bakedGI=true;settings.realtimeGI=false;settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;
  settings.lightmapResolution=12;settings.lightmapMaxSize=1024;settings.lightmapPadding=4;
  settings.directSampleCount=32;settings.indirectSampleCount=128;settings.environmentSampleCount=64;
  settings.directionalityMode=LightmapsMode.NonDirectional;Lightmapping.lightingSettings=settings;
  RenderSettings.sun.lightmapBakeType=LightmapBakeType.Baked;
  var pg=GameObject.Find("Fase4_LightProbes");if(!pg)pg=new GameObject("Fase4_LightProbes");var probes=pg.GetComponent<LightProbeGroup>();if(!probes) probes=pg.AddComponent<LightProbeGroup>();
  var points=new List<Vector3>();for(float x=-5;x<=5;x+=2.5f)for(float z=-2;z<=8;z+=2.5f)foreach(float y in new[]{.3f,1.2f,2.2f})points.Add(new Vector3(x,y,z));probes.probePositions=points.ToArray();
  foreach(var c in UnityEngine.Object.FindObjectsByType<Fase3.CellInteraction>(FindObjectsSortMode.None))foreach(var r in c.GetComponentsInChildren<MeshRenderer>()){GameObjectUtility.SetStaticEditorFlags(r.gameObject,0);r.lightProbeUsage=LightProbeUsage.BlendProbes;}
  Directory.CreateDirectory(A+"Settings");AssetDatabase.Refresh();string urpPath=A+"Settings/Quest_URP.asset";
  var urp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
  if(!urp){urp=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);AssetDatabase.CreateAsset(urp,urpPath);}
  urp.supportsHDR=false;urp.msaaSampleCount=4;urp.renderScale=1;var so=new SerializedObject(urp);so.FindProperty("m_MainLightShadowsSupported").boolValue=false;so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;so.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.PerPixel;so.ApplyModifiedPropertiesWithoutUndo();urp.maxAdditionalLightsCount=1;
  GraphicsSettings.defaultRenderPipeline=urp;QualitySettings.renderPipeline=urp;EditorUtility.SetDirty(urp);
  changes.Add("Meshes estáticos GI="+statics+"; probes="+points.Count+"; luz principal baked; luz de estado realtime sin sombras.");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllLines(Path.Combine(Fase4Audit.Output,"Cambios_configuracion.txt"),changes);
  var issues=new List<OpenXRFeature.ValidationRule>();OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
  File.WriteAllLines(Path.Combine(Fase4Audit.Output,"Validacion_OpenXR.txt"),issues.Select(i=>(i.error?"ERROR ":"WARNING ")+i.message));
  Debug.Log("F4_BAKE_BEGIN");bool ok=Lightmapping.Bake();
  EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText(Path.Combine(Fase4Audit.Output,"Horneado.txt"),"UTC="+DateTime.UtcNow.ToString("o")+"\nSuccess="+ok+"\nLightmaps="+LightmapSettings.lightmaps.Length+"\nStaticMeshes="+statics+"\nProbes="+(LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0));
  Debug.Log("F4_BAKE_END "+ok);
 }
 public static void BuildAPK() {
  UnityEditor.Android.UserBuildSettings.DebugSymbols.level=Unity.Android.Types.DebugSymbolLevel.SymbolTable;
  UnityEditor.Android.UserBuildSettings.DebugSymbols.format=Unity.Android.Types.DebugSymbolFormat.Zip;
  Directory.CreateDirectory(Fase4Audit.Output);var dst=Path.GetFullPath(Path.Combine(Application.dataPath,"../../F4Trabajo/APK/Fase4_Laboratorio_4.0.0_v400.apk"));
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Fase2Audit.ScenePath},locationPathName=dst,target=BuildTarget.Android,options=BuildOptions.None});
  var s=report.summary;
  File.WriteAllText(Path.Combine(Fase4Audit.Output,"Build_resumen.txt"),"Result="+s.result+"\nBytes="+s.totalSize+"\nErrors="+s.totalErrors+"\nWarnings="+s.totalWarnings+"\nDuration="+s.totalTime+"\nAPK="+dst);
  File.WriteAllLines(Path.Combine(Fase4Audit.Output,"Build_mensajes.txt"),report.steps.SelectMany(x=>x.messages).Select(x=>x.type+": "+x.content));
  if(s.result!=BuildResult.Succeeded)throw new Exception("F4 APK build failed");
 }
}
