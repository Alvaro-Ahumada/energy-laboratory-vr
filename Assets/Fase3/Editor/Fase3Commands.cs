using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
/// <summary>Handles local Editor commands for Fase3Commands.</summary>
[InitializeOnLoad]
public static class Fase3Commands
{
    static double next;
    static Fase3Commands(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+.5;
        var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Fase3.command"));if(!File.Exists(path))return;
        var command=File.ReadAllText(path).Trim();File.Delete(path);
        if(command=="Refresh")AssetDatabase.Refresh();
        if(command=="InspectCanvas") { EditorSceneManager.OpenScene(Fase2Audit.ScenePath); foreach(var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(c.name.Contains("Menu")){Selection.activeGameObject=c.gameObject;break;} }
        if(command=="InspectCell") { EditorSceneManager.OpenScene(Fase2Audit.ScenePath); Selection.activeGameObject=Object.FindFirstObjectByType<Fase3.CellInteraction>().gameObject; }
        if(command=="StableGrab") {
            EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
            int count=0;
            foreach(var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(b.GetType().Name=="InteractionAttachController") {
                    var so=new SerializedObject(b);so.FindProperty("m_UseDistanceBasedVelocityScaling").boolValue=false;so.FindProperty("m_UseMomentum").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();count++;
                }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
            File.WriteAllText(Path.Combine(Fase3Audit.Output,"Agarre_estable.txt"),"Amplificacion por velocidad desactivada en "+count+" controladores. Transporte 1:1; no lanzamiento. UTC="+System.DateTime.UtcNow.ToString("o"));
        }
        if(command=="FixLegacy") {
            EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
            int count=0;
            foreach(var cell in Object.FindObjectsByType<Fase3.CellInteraction>(FindObjectsSortMode.None))
                foreach(var legacy in cell.GetComponents<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>()) {legacy.enabled=false;EditorUtility.SetDirty(legacy);count++;}
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            File.WriteAllText(Path.Combine(Fase3Audit.Output,"Correccion_interactuables.txt"),"Componentes heredados desactivados: "+count+"\nUTC="+System.DateTime.UtcNow.ToString("o"));
        }
        if(command=="Input")Fase3InputProbe.Run();
        if(command=="Record"){SessionState.SetBool("F3Record",true);Fase3InputProbe.Run();}
        if(command=="Test")Fase3Tests.Run();
        if(command=="Build")Fase3Builder.Build();
        if(command=="Play"){EditorSceneManager.OpenScene(Fase2Audit.ScenePath);EditorApplication.isPlaying=true;}
        if(command=="Stop")EditorApplication.isPlaying=false;
        if(command=="Snapshot") {
            var s=Object.FindFirstObjectByType<Fase3.LabSession>();
            File.AppendAllText(Path.Combine(Fase3Audit.Output,"Estado.txt"),"UTC="+System.DateTime.UtcNow.ToString("o")+" Playing="+EditorApplication.isPlaying+" Session="+(s?s.State.ToString():"none")+" Count="+(s?s.Count:-1)+"\n");
        }
    }
}



// Import revision 2026-09-29
