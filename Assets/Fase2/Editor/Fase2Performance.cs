using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine.Profiling;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Fase2;

/// <summary>Profiles Editor playback for Fase2Performance.</summary>
[InitializeOnLoad]
public static class Fase2Performance
{
 public static string Output => Path.Combine(Fase2Audit.Output,"Retroalimentacion_2026_09_24");
 static Fase2Performance(){EditorApplication.playModeStateChanged+=State;}
 public static void OpenCapture(){
  EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
  EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
  ProfilerDriver.LoadProfile(Path.Combine(Output,"Sesion_Profiler.raw"),false);
  Debug.Log("FASE2_CAPTURE_IMPORTED");
 }
 [MenuItem("Fase 2/Medir rendimiento con Profiler")]
 /// <summary>Starts this Editor check.</summary>
 public static void Run(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Salir de Play antes de iniciar.");
  if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Guardar o descartar cambios de la escena antes de medir.");
  Directory.CreateDirectory(Output);
  EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
  SessionState.SetBool("Fase2PerformanceRun",true);
  EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
 }
 static void State(PlayModeStateChange state){
  if(!SessionState.GetBool("Fase2PerformanceRun",false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){new GameObject("Medicion_temporal_Profiler").AddComponent<Fase2PerformanceSampler>();}
  if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("Fase2PerformanceRun",false);EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");Debug.Log("FASE2_PERFORMANCE_FINISHED "+Output);}
 }
}

/// <summary>Profiles Editor playback for Fase2PerformanceSampler.</summary>
public class Fase2PerformanceSampler:MonoBehaviour
{
 Dictionary<string,ProfilerRecorder> rec=new Dictionary<string,ProfilerRecorder>();
 StringBuilder csv=new StringBuilder();
 List<string> events=new List<string>();
 EnergyLab lab;EnergyCell[] cells;EnergyStation station;XRInteractionManager manager;XRBaseInteractor interactor;XRInteractionSimulator simulator;
 double started;int step;bool recording;bool done;bool moving;int errors;int originalTarget;bool originalBackground;
 string[] wanted={"Main Thread","Render Thread","GC Allocated In Frame","Total Used Memory","GC Used Memory","Draw Calls Count","Batches Count","SetPass Calls Count","Triangles Count","GPU Frame Time"};
 void Start(){
  originalTarget=Application.targetFrameRate;originalBackground=Application.runInBackground;Application.runInBackground=true;
  started=Time.realtimeSinceStartupAsDouble;
  lab=FindFirstObjectByType<EnergyLab>();cells=FindObjectsByType<EnergyCell>(FindObjectsSortMode.None).OrderBy(c=>c.cellId).ToArray();station=FindFirstObjectByType<EnergyStation>();
  manager=FindFirstObjectByType<XRInteractionManager>();interactor=FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);simulator=FindFirstObjectByType<XRInteractionSimulator>();
  if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();InputSystem.onBeforeUpdate+=KeyboardInput;Application.logMessageReceived+=OnLog;
  File.WriteAllText(Path.Combine(Fase2Performance.Output,"Condiciones.txt"),"UTC="+DateTime.UtcNow.ToString("o")+"\nUnity="+Application.unityVersion+"\nCPU="+SystemInfo.processorType+"\nCPU_threads="+SystemInfo.processorCount+"\nRAM_MB="+SystemInfo.systemMemorySize+"\nGPU="+SystemInfo.graphicsDeviceName+"\nGraphicsAPI="+SystemInfo.graphicsDeviceType+"\nGPU_MB="+SystemInfo.graphicsMemorySize+"\nGame="+Screen.width+"x"+Screen.height+"\nVSync="+QualitySettings.vSyncCount+"\nTargetFPS="+Application.targetFrameRate+"\nBuildTarget="+EditorUserBuildSettings.activeBuildTarget+"\nSimulador="+(simulator!=null)+"\nEscena="+EditorSceneManager.GetActiveScene().path+"\nWarmup=10 s; medicion=80 s. Editor con renderizado, sin visor. Selecciones automatizadas por gestor XR; movimiento con entrada W simulada. Sin Deep Profile.\n");
 }
 void KeyboardInput(){if(moving&&Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));}
 void OnLog(string s,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors++;events.Add("ERROR "+s);}}
 /// <summary>Resets the cells and starts a new timed round.</summary>
 void Begin(){
  ProfilerDriver.profileEditor=false;ProfilerDriver.deepProfiling=false;
  Profiler.logFile=Path.Combine(Fase2Performance.Output,"Sesion_Profiler.raw");Profiler.enableBinaryLog=true;Profiler.enabled=true;
  var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
  var inventory=new StringBuilder();
  foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);inventory.AppendLine(d.Name+" | "+d.Category+" | "+d.UnitType);if(wanted.Contains(d.Name)&&!rec.ContainsKey(d.Name))rec.Add(d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1));}
  File.WriteAllText(Path.Combine(Fase2Performance.Output,"Contadores_disponibles.txt"),inventory.ToString());
  csv.AppendLine("elapsed_s,frame,phase,frame_ms,"+string.Join(",",rec.Keys));recording=true;events.Add("START "+DateTime.UtcNow.ToString("o"));
 }
 void Select(XRSimpleInteractable target){manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)target);manager.SelectExit((IXRSelectInteractor)interactor,(IXRSelectInteractable)target);}
 void Update(){
  if(done)return;double elapsed=Time.realtimeSinceStartupAsDouble-started;
  if(!recording){if(elapsed>=10)Begin();else return;}
  double t=elapsed-10;
  string phase=t<15?"reposo":t<30?"recorrido":t<50?"recoleccion":t<60?"activacion":"estabilizacion";
  csv.Append(t.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(Time.frameCount).Append(',').Append(phase).Append(',').Append((Time.unscaledDeltaTime*1000).ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
  foreach(var r in rec.Values)csv.Append(',').Append(r.Valid?r.LastValue:-1);csv.AppendLine();
  if(step==0&&t>=15){moving=true;events.Add("15s movimiento antes="+Camera.main.transform.position);step++;}
  if(step==1&&t>=17){moving=false;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());events.Add("17s movimiento despues="+Camera.main.transform.position);step++;}
  if(step==2&&t>=30){Select(cells[0].GetComponent<XRSimpleInteractable>());events.Add("30s celda1="+cells[0].Collected);step++;}
  if(step==3&&t>=37){Select(cells[1].GetComponent<XRSimpleInteractable>());events.Add("37s celda2="+cells[1].Collected);step++;}
  if(step==4&&t>=44){Select(cells[2].GetComponent<XRSimpleInteractable>());events.Add("44s celda3="+cells[2].Collected);step++;}
  if(step==5&&t>=50){Select(station.GetComponent<XRSimpleInteractable>());events.Add("50s estacion="+lab.Activated+" count="+lab.Count);step++;}
  if(t>=80)Finish();
 }
 /// <summary>Freezes the clock, locks interactions and displays the round outcome.</summary>
 void Finish(){
  done=true;Profiler.enabled=false;Profiler.enableBinaryLog=false;Profiler.logFile="";
  foreach(var r in rec.Values)r.Dispose();rec.Clear();
  File.WriteAllText(Path.Combine(Fase2Performance.Output,"Muestras_Profiler.csv"),csv.ToString());
  events.Add("Errores="+errors);File.WriteAllLines(Path.Combine(Fase2Performance.Output,"Eventos.txt"),events);
  Application.runInBackground=originalBackground;Application.targetFrameRate=originalTarget;
  EditorApplication.isPlaying=false;
 }
 void OnDestroy(){moving=false;InputSystem.onBeforeUpdate-=KeyboardInput;Application.logMessageReceived-=OnLog;foreach(var r in rec.Values)r.Dispose();if(recording&&!done){Profiler.enabled=false;Profiler.enableBinaryLog=false;File.WriteAllText(Path.Combine(Fase2Performance.Output,"INTERRUMPIDA.txt"),"Medicion interrumpida; no usar como resultado final.");}}
}
