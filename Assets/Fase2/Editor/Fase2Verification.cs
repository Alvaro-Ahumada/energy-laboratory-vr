using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Fase2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.Rendering.Universal;

/// <summary>Checks the original laboratory interactions for Fase2Verification.</summary>
[InitializeOnLoad]
public static class Fase2Verification
{
    static double start;
    static int step;
    static Vector3 before;
    static List<string> lines=new List<string>();
    static Camera evidenceCamera;
    static int errors;
    static bool holdW;
    static void HoldKeyboard(){if(holdW&&Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));}
    static Fase2Verification(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Fase 2/Verificar funcionamiento")]
    /// <summary>Starts this Editor check.</summary>
    public static void Run()
    {
        AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.universal/Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/VirtualOffset/TraceVirtualOffset.urtshader",ImportAssetOptions.ForceUpdate);
        var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        var area=UnityEngine.Object.FindFirstObjectByType<TeleportationArea>();area.interactionLayers=1<<31;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        SessionState.SetBool("F2Verify",true);EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool("F2Verify",false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground=true;
            if(Keyboard.current==null)InputSystem.AddDevice<Keyboard>();
            start=EditorApplication.timeSinceStartup;step=0;errors=0;lines.Clear();Application.logMessageReceived+=Log;
            InputSystem.onBeforeUpdate+=HoldKeyboard;
            lines.Add("UTC="+DateTime.UtcNow.ToString("o")+"; Unity="+Application.unityVersion+"; GPU="+SystemInfo.graphicsDeviceName);
            lines.Add("Prueba automatizada en Editor con Android como destino; sin visor físico.");
            EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
            var lab=UnityEngine.Object.FindFirstObjectByType<EnergyLab>();
            lines.Add("REAPERTURA laboratorio="+(lab!=null)+" celdas="+UnityEngine.Object.FindObjectsByType<EnergyCell>(FindObjectsSortMode.None).Length+" count="+lab.Count);
            lines.Add("Errores/excepciones durante prueba="+errors);
            File.WriteAllLines(Path.Combine(Fase2Audit.Output,"Pruebas_automaticas.txt"),lines);
            SessionState.SetBool("F2Verify",false);Application.logMessageReceived-=Log;
            holdW=false;InputSystem.onBeforeUpdate-=HoldKeyboard;
            if(Application.isBatchMode)EditorApplication.Exit(errors==0?0:2);
        }
    }
    static void Log(string condition,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;lines.Add("ERROR "+condition);}}
    static void Check(string label,bool ok,string detail=""){lines.Add((ok?"PASS ":"FAIL ")+label+" "+detail);if(!ok)errors++;}
    static void Select(XRSimpleInteractable target)
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
        var interactor=UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled);
        manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)target);
        manager.SelectExit((IXRSelectInteractor)interactor,(IXRSelectInteractable)target);
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;
        EditorApplication.QueuePlayerLoopUpdate();
        if(EditorApplication.isPaused){lines.Add("AJUSTE: Editor estaba pausado; se reanuda la prueba.");EditorApplication.isPaused=false;start=EditorApplication.timeSinceStartup;return;}
        double elapsed=EditorApplication.timeSinceStartup-start;
        try
        {
            var lab=UnityEngine.Object.FindFirstObjectByType<EnergyLab>();
            var cells=UnityEngine.Object.FindObjectsByType<EnergyCell>(FindObjectsSortMode.None).OrderBy(c=>c.cellId).ToArray();
            if(step==0&&elapsed>5)
            {
                Check("Inicio",lab!=null&&cells.Length==3);
                lines.Add("DIAGNOSTICO time="+Time.time+" frames="+Time.frameCount+" delta="+Time.deltaTime+" paused="+EditorApplication.isPaused);
                var sim=UnityEngine.Object.FindFirstObjectByType<XRInteractionSimulator>();Check("Simulador instanciado",sim!=null);
                int missing=0;foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                Check("Sin scripts faltantes",missing==0,"cantidad="+missing);
                var meshes=lab.GetComponentsInChildren<MeshFilter>();Check("Geometria importada",meshes.Length>40);
                foreach(var c in cells)Check("Particulas "+c.cellId,c.halo.isPlaying&&c.halo.particleCount>0,"vivas="+c.halo.particleCount);
                Check("Musica",GameObject.Find("Musica_Ambiente").GetComponent<AudioSource>().isPlaying);
                Check("Skybox",RenderSettings.skybox!=null);
                Check("Bloqueo estacion sin celdas",!lab.TryActivate()&&!lab.Activated);
                var go=new GameObject("Camara_Evidencia_Temporal");evidenceCamera=go.AddComponent<Camera>();evidenceCamera.enabled=false;evidenceCamera.fieldOfView=58;evidenceCamera.nearClipPlane=.03f;evidenceCamera.farClipPlane=60;go.AddComponent<UniversalAdditionalCameraData>();
                Capture("Unity_vista_general.png",new Vector3(8,8,-7),new Vector3(0,.6f,3));
                Capture("Unity_celda_particulas.png",new Vector3(-3,1.6f,.4f),new Vector3(-3,1.3f,2));
                before=Camera.main.transform.position;
                holdW=true;
                var za=sim.translateZInput.inputActionReference;
                lines.Add("AccionZ ref="+(za!=null)+" action="+(za!=null&&za.action!=null)+" source="+sim.translateZInput.inputSourceMode);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.W));
                lines.Add("INPUT simEnabled="+sim.enabled+" fps="+sim.manipulatingFPS+" value="+sim.translateZInput.ReadValue());
                step++;start=EditorApplication.timeSinceStartup;
            }
            else if(step==1&&elapsed>1.2)
            {
                lines.Add("INPUT despues valor="+UnityEngine.Object.FindFirstObjectByType<XRInteractionSimulator>().translateZInput.ReadValue()+" tecla="+Keyboard.current.wKey.isPressed);
                holdW=false;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                var after=Camera.main.transform.position;Check("Movimiento simulador W",Vector3.Distance(before,after)>.05f,"antes="+before+" despues="+after);
                foreach(var c in cells){Select(c.GetComponent<XRSimpleInteractable>());Check("Seleccion XR celda "+c.cellId,c.Collected);Check("Antiduplicacion "+c.cellId,!c.Collect());Check("Audio celda "+c.cellId,c.audioSource.isPlaying);}
                Check("Conteo exacto",lab.Count==3,"count="+lab.Count);
                Select(UnityEngine.Object.FindFirstObjectByType<EnergyStation>().GetComponent<XRSimpleInteractable>());
                Check("Activacion XR estacion",lab.Activated&&lab.ActivationCount==1);
                Check("Antiduplicacion estacion",!lab.TryActivate()&&lab.ActivationCount==1);
                Check("Audio estacion",lab.stationAudio.isPlaying);
                Capture("Unity_estacion_activada.png",new Vector3(0,2,5.8f),new Vector3(0,2,8));
                var provider=UnityEngine.Object.FindFirstObjectByType<TeleportationProvider>();before=Camera.main.transform.position;
                Check("Solicitud teletransporte",provider!=null&&provider.QueueTeleportRequest(new TeleportRequest{destinationPosition=new Vector3(2,0,4),destinationRotation=Quaternion.identity,matchOrientation=MatchOrientation.WorldSpaceUp}));
                step++;start=EditorApplication.timeSinceStartup;
            }
            else if(step==2&&elapsed>2)
            {
                var pos=Camera.main.transform.position;Check("Teletransporte proveedor heredado",Vector2.Distance(new Vector2(pos.x,pos.z),new Vector2(2,4))<.15f,"camara="+pos);
                foreach(var c in cells)Check("Colision celda recogida desactivada "+c.cellId,!c.GetComponent<Collider>().enabled);
                Check("UI progreso",lab.progress.text.Contains("ACTIVADO"));
                var cc=UnityEngine.Object.FindFirstObjectByType<CharacterController>();Check("CharacterController heredado",cc!=null);
                Check("Suelo collider",Physics.Raycast(new Vector3(0,1,3),Vector3.down,out var hit,2)&&hit.collider.name=="Suelo_Navegable");
                File.WriteAllLines(Path.Combine(Fase2Audit.Output,"Pruebas_automaticas.txt"),lines);
                EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            }
        }
        catch(Exception ex){lines.Add("EXCEPTION "+ex);errors++;File.WriteAllLines(Path.Combine(Fase2Audit.Output,"Pruebas_automaticas.txt"),lines);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
    }
    static void Capture(string name,Vector3 pos,Vector3 target)
    {
        evidenceCamera.transform.position=pos;evidenceCamera.transform.LookAt(target);
        var rt=new RenderTexture(1600,900,24);evidenceCamera.targetTexture=rt;evidenceCamera.Render();
        var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Fase2Audit.Output,name),image.EncodeToPNG());
        RenderTexture.active=previous;evidenceCamera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}
