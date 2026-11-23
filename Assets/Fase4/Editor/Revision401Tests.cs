using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Fase3;

/// <summary>Exercises the session and records observed results for Revision401Tests.</summary>
[InitializeOnLoad]
public static class Revision401Tests
{
    static string Output => Path.GetFullPath(Path.Combine(Application.dataPath,Environment.GetCommandLineArgs().Contains("-rev402") ? "../../F4Trabajo/Revision402/Pruebas" : "../../F4Trabajo/Revision401/Pruebas"));
    static int step;static double since;static List<string> rows=new List<string>();static LabSession lab;
    static Revision401Tests(){EditorApplication.playModeStateChanged+=State;}
    
    /// <summary>Starts this Editor check.</summary>
    public static void Run(){Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(Fase2Audit.ScenePath);SessionState.SetBool("Revision401Test",true);EditorApplication.isPlaying=true;}
    static void State(PlayModeStateChange s)
    {
        if(!SessionState.GetBool("Revision401Test",false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){step=0;since=EditorApplication.timeSinceStartup;rows.Clear();Application.runInBackground=true;EditorApplication.update+=Tick;}
        if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("Revision401Test",false);EditorApplication.update-=Tick;File.WriteAllLines(Path.Combine(Output,"Pruebas_automaticas.txt"),rows);if(Application.isBatchMode) EditorApplication.Exit(rows.Any(r=>r.StartsWith("FAIL")||r.StartsWith("EXCEPTION"))?1:0);}
    }
    static void Check(string name,bool pass,string detail=""){rows.Add((pass?"PASS ":"FAIL ")+name+" | "+detail);File.WriteAllLines(Path.Combine(Output,"Pruebas_automaticas.txt"),rows);}
    static void Next(){step++;since=EditorApplication.timeSinceStartup;}
    static int frames; static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if (EditorApplication.isPaused) { Debug.LogWarning("Test resumed a paused Editor at step " + step); EditorApplication.isPaused=false; }
        EditorApplication.QueuePlayerLoopUpdate();
        if(step==6 && lab.State==LabState.Win && frames<40){if(EditorApplication.timeSinceStartup-since<.1)return;since=EditorApplication.timeSinceStartup;Camera.main.transform.root.Rotate(Vector3.up,3f);Capture("Giro_"+frames.ToString("D3")+".png");frames++;return;} if(EditorApplication.timeSinceStartup-since<2)return;
        try{
            if(step==0){
                lab=UnityEngine.Object.FindFirstObjectByType<LabSession>();Check("Inicio menu",lab&&lab.State==LabState.Menu);
                Check("Simulador",UnityEngine.Object.FindFirstObjectByType<XRInteractionSimulator>());
                Check("Unico interactuable activo por celda",lab.cells.All(c=>c.GetComponents<XRBaseInteractable>().Count(x=>x.enabled)==0),"Menu mantiene grab desactivado; legacy debe estar desactivado");
                Check("Un gestor UI",UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1);
                Check("Un AudioListener",UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1);
                Check("Canvas World Space",lab.GetComponentsInChildren<Canvas>(true).Length==5&&lab.GetComponentsInChildren<Canvas>(true).All(c=>c.renderMode==RenderMode.WorldSpace));
                Check("Duracion guardada",Mathf.Approximately(lab.durationSeconds,90));
                Check("Contador cero",lab.Count==0);Capture("01_Menu.png");lab.ShowCredits();Next();
            }else if(step==1){Check("Creditos",lab.credits.activeSelf&&!lab.menu.activeSelf);Capture("02_Creditos.png");lab.CloseCredits();lab.Begin();Next();
            }else if(step==2){
                Check("Inicio temporizador",lab.Remaining<90&&lab.Remaining>85,"remaining="+lab.Remaining);Capture("03_HUD.png");
                var manager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
                var interactor=UnityEngine.Object.FindObjectsByType<XRBaseInteractor>(FindObjectsSortMode.None).First(i=>i.isActiveAndEnabled&&!(i is XRSocketInteractor));
                var c=lab.cells[0];manager.SelectEnter((IXRSelectInteractor)interactor,c.Grab);Check("Grab por gestor XR",c.Grab.isSelected);
                manager.SelectExit((IXRSelectInteractor)interactor,c.Grab);Check("Release por gestor XR",!c.Grab.isSelected);Check("Grab no cuenta",lab.Count==0);
                Next();
            }else if(step==3){Place(0);Next();
            }else if(step==4){Check("Socket 1 por fisica",lab.Count==1,"count="+lab.Count);Check("Antiduplicacion",lab.Count==1&&!lab.Accept(lab.cells[0])&&lab.Count==1);Place(1);Next();
            }else if(step==5){Check("Socket 2",lab.Count==2,"count="+lab.Count);Place(2);Next();
            }else if(step==6){Check("Victoria",lab.State==LabState.Win&&lab.Count==3,"state="+lab.State);Check("Celdas bloqueadas",lab.cells.All(c=>!c.Grab.enabled));Check("Panel frontal tras giro de 120 grados",Vector3.Angle(Camera.main.transform.forward,lab.win.transform.position-Camera.main.transform.position)<35);Capture("04_Victoria.png");lab.Begin(); var rig401=Camera.main.transform.root;var rot401=rig401.rotation;var pos401=rig401.position;rig401.position+=Vector3.forward*5;rig401.rotation=Quaternion.Euler(0,180,0);Capture("Muro.png");if(Environment.GetCommandLineArgs().Contains("-rev402")){rig401.position+=new Vector3(-3,0,-2);rig401.rotation=Quaternion.Euler(0,153,0);Capture("Muro_detalle.png");}rig401.rotation=rot401;rig401.position=pos401;Next();
            }else if(step==7){
                Check("Reinicio victoria",lab.Count==0&&lab.State==LabState.Playing&&lab.cells.All(c=>!c.Accepted));
                lab.cells[0].transform.position=new Vector3(0,-2,0);Next();
            }else if(step==8){Check("Recuperacion",lab.cells[0].transform.position.y>0);Check("Mensaje personalizado",lab.hint.text=="La celda regresó a su pedestal. Puedes intentarlo de nuevo.",lab.hint.text);Capture("Personalizacion_manual_resultado.png");lab.ReturnToMenu();Check("Menu sin partida",lab.State==LabState.Menu);lab.durationSeconds=3;lab.Begin();Next();
            }else if(step==9){Next();
            }else if(step==10){Check("Derrota reloj real prueba 3s",lab.State==LabState.Lose&&lab.Count==0,"state="+lab.State);Capture("05_Derrota.png");lab.Begin();Check("Reinicio derrota",lab.State==LabState.Playing&&lab.Count==0);
                var deadline=typeof(LabSession).GetField("deadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                lab.Accept(lab.cells[0]);lab.Accept(lab.cells[1]);
                deadline.SetValue(lab,Time.timeAsDouble);Check("Limite exacto rechaza ultima celda",!lab.Accept(lab.cells[2])&&lab.State==LabState.Lose&&lab.Count==2);
                for(int i=0;i<5;i++){lab.Begin();lab.ReturnToMenu();}Check("Cinco reinicios sin conteo residual",lab.Count==0&&lab.State==LabState.Menu&&lab.cells.All(c=>!c.Accepted));
                EditorApplication.isPlaying=false;}
        }catch(Exception ex){rows.Add("EXCEPTION "+ex);File.WriteAllLines(Path.Combine(Output,"Pruebas_automaticas.txt"),rows);EditorApplication.isPlaying=false;}
    }
    static void Place(int i){var c=lab.cells[i];var b=c.GetComponent<Rigidbody>();b.position=lab.sockets[i].transform.position;c.transform.position=b.position;b.linearVelocity=Vector3.zero;b.angularVelocity=Vector3.zero;}
    public static void Capture(string name)
    {
        var cam=Camera.main;if(!cam)return;
        var rt=new RenderTexture(1920,1080,24);var prior=cam.targetTexture;cam.targetTexture=rt;cam.Render();
        var active=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Output,name),tex.EncodeToPNG());
        RenderTexture.active=active;cam.targetTexture=prior;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}
