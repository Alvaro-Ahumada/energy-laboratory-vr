using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
/// <summary>Inspects scene configuration for Fase3Audit.</summary>
[InitializeOnLoad]
public static class Fase3Audit {
 public static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../F3Trabajo/Evidencias"));
 static double started; static bool running;
 static Fase3Audit(){EditorApplication.update+=Tick;}
 /// <summary>Collects the initial scene and configuration evidence.</summary>
 public static void Baseline(){
 Directory.CreateDirectory(Output);
 var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
 int missing=scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
 File.WriteAllText(Path.Combine(Output,"Baseline.txt"),"UTC="+DateTime.UtcNow.ToString("o")+"\nUnity="+Application.unityVersion+"\nPlatform="+EditorUserBuildSettings.activeBuildTarget+"\nScene="+scene.path+"\nMissing="+missing+"\n");
 Application.logMessageReceived+=Log;
 started=EditorApplication.timeSinceStartup;running=true;EditorApplication.isPlaying=true;
 }
 static void Log(string c,string s,LogType t){if(t==LogType.Error||t==LogType.Exception)File.AppendAllText(Path.Combine(Output,"Baseline_errores.txt"),t+": "+c+"\n"+s+"\n");}
 static void Tick(){if(!running||EditorApplication.timeSinceStartup-started<25)return;running=false;File.AppendAllText(Path.Combine(Output,"Baseline.txt"),"Play="+EditorApplication.isPlaying+"\nCells="+UnityEngine.Object.FindObjectsByType<Fase2.EnergyCell>(FindObjectsSortMode.None).Length+"\n");EditorApplication.isPlaying=false;Application.logMessageReceived-=Log;Debug.Log("FASE3_BASELINE_FINISHED");}
}
