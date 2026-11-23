using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using Fase3;

// Test instrumentation only. Drives simulator input, never invokes UI onClick or game transitions.
/// <summary>Drives simulated input and records outcomes for Fase3InputProbe.</summary>
[InitializeOnLoad]
public static class Fase3InputProbe
{
    static XRInteractionSimulator sim; static LabSession lab; static int step; static double since;
    static Key[] held=Array.Empty<Key>(); static Vector3 handPos,lookAt; static bool driving;
    static Vector3 beforeTeleport;static Quaternion beforeTurn;
    static bool recording;static Vector3 currentHand;static bool handInitialized;
    static double Clock=>recording?Time.timeAsDouble:EditorApplication.timeSinceStartup;
    static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static Fase3InputProbe(){EditorApplication.playModeStateChanged+=State;}
    /// <summary>Starts this Editor check.</summary>
    public static void Run(){EditorSceneManager.OpenScene(Fase2Audit.ScenePath);SessionState.SetBool("F3Input",true);EditorApplication.isPlaying=true;}
    static void State(PlayModeStateChange s)
    {
        if(!SessionState.GetBool("F3Input",false))return;
        if(s==PlayModeStateChange.EnteredPlayMode){recording=SessionState.GetBool("F3Record",false);SessionState.SetBool("F3Record",false);step=0;since=Clock;driving=false;handInitialized=false;if(recording)new GameObject("Captura_temporal_editor").AddComponent<Fase3Recorder>();Application.runInBackground=true;EditorApplication.update+=Tick;InputSystem.onBeforeUpdate+=Input;File.WriteAllText(Path.Combine(Fase3Audit.Output,"Prueba_entrada.txt"),"Prueba automatizada: teclas T/G -> XRInteractionSimulator -> controles XR -> raycast/botones/grab. Poses programadas del controlador para repetibilidad; no recorrido manual.\n");}
        if(s==PlayModeStateChange.EnteredEditMode){SessionState.SetBool("F3Input",false);EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;held=Array.Empty<Key>();}
    }
    static void Input(){if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(held));}
    static void Log(string text){File.AppendAllText(Path.Combine(Fase3Audit.Output,"Prueba_entrada.txt"),"t="+Time.time.ToString("F2")+" "+text+"\n");}
    static void Aim(Transform target){handPos=Camera.main.transform.position+new Vector3(.3f,-.2f,.3f);lookAt=target.position;driving=true;}
    static void Drive()
    {
        if(!driving||!sim)return;
        var parent=Camera.main.transform.parent;
        if(!handInitialized){currentHand=handPos;handInitialized=true;}
        currentHand=recording?Vector3.MoveTowards(currentHand,handPos,3f*Time.deltaTime):handPos;
        var field=typeof(XRInteractionSimulator).GetField("m_RightControllerState",flags);
        object state=field.GetValue(sim);var type=state.GetType();
        var rotation=Quaternion.LookRotation(lookAt-handPos);
        type.GetField("devicePosition").SetValue(state,parent.InverseTransformPoint(currentHand));
        type.GetField("deviceRotation").SetValue(state,Quaternion.Inverse(parent.rotation)*rotation);
        field.SetValue(sim,state);typeof(XRInteractionSimulator).GetField("m_RightControllerEuler",flags).SetValue(sim,(Quaternion.Inverse(parent.rotation)*rotation).eulerAngles);
        if(recording && step!=35 && step!=36){
            Vector3 focus=lookAt;
            if(lab.State==LabState.Menu)focus=lab.menu.transform.position;
            else if(lab.State==LabState.Credits)focus=lab.credits.transform.position;
            else if(lab.State==LabState.Win)focus=lab.win.transform.position;
            else if(lab.State==LabState.Lose)focus=lab.lose.transform.position;
            else if(step>=37)focus=lab.hud.transform.position;
            var hmd=typeof(XRInteractionSimulator).GetField("m_HMDState",flags);object head=hmd.GetValue(sim);var ht=head.GetType();
            var desired=Quaternion.Inverse(parent.rotation)*Quaternion.LookRotation(focus-Camera.main.transform.position);
            var q=Quaternion.Slerp((Quaternion)ht.GetField("centerEyeRotation").GetValue(head),desired,Time.deltaTime*3);
            ht.GetField("centerEyeRotation").SetValue(head,q);ht.GetField("deviceRotation").SetValue(head,q);hmd.SetValue(sim,head);
            var angles=q.eulerAngles;if(angles.x>180)angles.x-=360;if(angles.y>180)angles.y-=360;if(angles.z>180)angles.z-=360;
            typeof(XRInteractionSimulator).GetField("m_CenterEyeEuler",flags).SetValue(sim,angles);
        }
    }
    static void Next(){step++;since=Clock;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
        try{
            Drive();if(Clock-since<(step==35?.08:(recording?2.5:1.5)))return;
            if(step==0){sim=UnityEngine.Object.FindFirstObjectByType<XRInteractionSimulator>();lab=UnityEngine.Object.FindFirstObjectByType<LabSession>();Aim(lab.menu.transform.Find("Créditos"));Log("Aim Credits; trigger action="+sim.triggerInput.ReadIsPerformed());Next();}
            else if(step==1){held=new[]{Key.T};Next();}
            else if(step==2){held=Array.Empty<Key>();Next();}
            else if(step==3){Log("Credits="+lab.State);Fase3Tests.Capture("Entrada_Creditos.png");Aim(lab.credits.transform.Find("Volver al menú"));Next();}
            else if(step==4){held=new[]{Key.T};Next();}
            else if(step==5){held=Array.Empty<Key>();Next();}
            else if(step==6){Log("Return="+lab.State);Aim(lab.menu.transform.Find("Iniciar juego"));Next();}
            else if(step==7){held=new[]{Key.T};Next();}
            else if(step==8){held=Array.Empty<Key>();Next();}
            else if(step==9){Log("Start="+lab.State);Aim(lab.cells[0].transform);lookAt+=Vector3.up*.22f;handPos=lookAt+new Vector3(0,.15f,-.7f);Next();}
            else if(step==10){Log("Physics="+(Physics.Raycast(handPos,(lookAt-handPos).normalized,out var hit,10)?hit.collider.name:"none"));Log("Hover="+lab.cells[0].Grab.isHovered+" Hint="+lab.hint.text);Fase3Tests.Capture("Entrada_Hover.png");held=new[]{Key.G};Next();}
            else if(step==11){Log("Grab="+lab.cells[0].Grab.isSelected+" position="+lab.cells[0].transform.position);Fase3Tests.Capture("Entrada_Grab.png");held=Array.Empty<Key>();Next();}
            else if(step==12){Log("Release="+!lab.cells[0].Grab.isSelected+" Count="+lab.Count);Next();}
            else if(step>=13 && step<28){
                int i=(step-13)/5;int action=(step-13)%5;var cell=lab.cells[i];
                if(action==0){Aim(cell.transform);lookAt+=Vector3.up*.22f;handPos=lookAt+new Vector3(0,.15f,-.7f);}
                if(action==1){held=new[]{Key.G};}
                if(action==2){Log("Transport grab "+i+"="+cell.Grab.isSelected);var delta=lab.sockets[i].transform.position-cell.transform.position;handPos+=delta;lookAt+=delta;}
                if(action==3){Log("Before release "+i+" position="+cell.transform.position);held=Array.Empty<Key>();}
                if(action==4){Log("Socket "+i+" accepted="+cell.Accepted+" Count="+lab.Count+" State="+lab.State);Fase3Tests.Capture("Entrada_Socket_"+i+".png");}
                Next();
            }
            else if(step==28){Log("End-to-end win="+(lab.State==LabState.Win));Aim(lab.win.transform.Find("Reiniciar"));Next();}
            else if(step==29){foreach(var nf in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsSortMode.None)){nf.TryGetCurrentUIRaycastResult(out var hit);Log("UI restart interactor="+nf.name+" selected="+nf.hasSelection+" hit="+(hit.gameObject?hit.gameObject.name:"none")+" position="+nf.transform.position+" forward="+nf.transform.forward);}Fase3Tests.Capture("Entrada_Apuntar_Reinicio.png");held=new[]{Key.T};Next();}
            else if(step==30){held=Array.Empty<Key>();Next();}
            else if(step==31){Log("Restart Win="+lab.State+" count="+lab.Count);beforeTeleport=Camera.main.transform.position;handPos=beforeTeleport+new Vector3(.3f,-.2f,.3f);lookAt=new Vector3(2,0,1);Next();}
            else if(step==32){held=new[]{Key.I};Next();}
            else if(step==33){held=Array.Empty<Key>();Next();}
            else if(step==34){Log("Teleport from="+beforeTeleport+" to="+Camera.main.transform.position+" moved="+(Vector3.Distance(beforeTeleport,Camera.main.transform.position)>.3f));beforeTurn=Camera.main.transform.rotation;held=new[]{Key.L};Next();}
            else if(step==35){held=Array.Empty<Key>();Next();}
            else if(step==36){Log("Snap angle="+Quaternion.Angle(beforeTurn,Camera.main.transform.rotation));Next();}
            else if(step==37){if(lab.State!=LabState.Lose){if(lab.State!=LabState.Playing){Log("FAIL second game not active");EditorApplication.isPlaying=false;}return;}Log("Natural defeat duration="+lab.durationSeconds+" remaining="+lab.Remaining);Fase3Tests.Capture("Entrada_Derrota90.png");Aim(lab.lose.transform.Find("Reiniciar"));Next();}
            else if(step==38){held=new[]{Key.T};Next();}
            else if(step==39){held=Array.Empty<Key>();Next();}
            else if(step==40){Log("Restart Lose="+lab.State+" count="+lab.Count);Aim(lab.hud.transform.Find("Volver al menú"));Next();}
            else if(step==41){held=new[]{Key.T};Next();}
            else if(step==42){held=Array.Empty<Key>();Next();}
            else if(step==43){Log("HUD Return="+lab.State);Aim(lab.menu.transform.Find("Salir"));Next();}
            else if(step==44){Log("Exit via trigger requested");held=new[]{Key.T};Next();}
            else if(step==45){held=Array.Empty<Key>();Next();}
            else if(step==46){Log("FAIL exit did not stop Play after trigger release");EditorApplication.isPlaying=false;}
        }catch(Exception e){Log("EXCEPTION "+e);held=Array.Empty<Key>();EditorApplication.isPlaying=false;}
    }
}
