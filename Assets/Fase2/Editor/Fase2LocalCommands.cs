using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Handles local Editor commands for Fase2LocalCommands.</summary>
[InitializeOnLoad]
public static class Fase2LocalCommands
{
    static double next;
    static Fase2LocalCommands(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        next=EditorApplication.timeSinceStartup+1;
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Fase2.command"));
        if(!File.Exists(path))return;
        var command=File.ReadAllText(path).Trim();File.Delete(path);
        if(command=="Repair")Repair();
        if(command=="Run")Fase2Verification.Run();
        if(command=="LoadProfiler"){
            var capture=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Fase 2 (Construcción del entorno de realidad virtual)/Desarrollo/Evidencias/Retroalimentacion_2026_09_24/Sesion_Profiler.raw"));
            UnityEditorInternal.ProfilerDriver.LoadProfile(capture,false);
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler");
            Debug.Log("FASE2_PROFILER_LOADED "+capture);
        }
        if(command=="RepairUI"){
            foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Packages/com.unity.learn.")&&(p.EndsWith(".uxml")||p.EndsWith(".uss"))))
                AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("FASE2_UI_REIMPORT_DONE");
        }
        if(command=="InputAudit"){
            var result=new System.Text.StringBuilder();
            foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".inputactions"))){
                result.AppendLine("PATH "+p);
                foreach(var o in AssetDatabase.LoadAllAssetsAtPath(p)){
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o,out string guid,out long id);
                    result.AppendLine(o.GetType().Name+" "+o.name+" "+id+" "+guid);
                }
            }
            File.WriteAllText(Path.Combine(Fase2Audit.Output,"InputAudit.txt"),result.ToString());
        }
        if(command=="RepairInputs"){
            foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".inputactions")))
                AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Samples/XR Interaction Toolkit/")&&p.EndsWith(".prefab")))
                AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("FASE2_INPUT_REIMPORT_DONE");
        }
        if(command=="Polish"){
            var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
            var sign=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.text.StartsWith("EXPLORA"));
            if(sign){sign.transform.position=new Vector3(-1.3f,2.4f,3.5f);sign.fontSize=.6f;sign.rectTransform.sizeDelta=new Vector2(2.7f,1);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Path.Combine(Fase2Audit.Output,"Ajuste_rotulo.txt"),"Rotulo="+(sign?sign.transform.position.ToString():"NO ENCONTRADO")+" UTC="+DateTime.UtcNow.ToString("o"));
        }
        if(command=="Play"){EditorApplication.isPaused=false;EditorApplication.isPlaying=true;}
        if(command=="Stop")EditorApplication.isPlaying=false;
        if(command=="Snapshot"){
            var c=Camera.main;var sim=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>();
            File.AppendAllText(Path.Combine(Fase2Audit.Output,"Prueba_manual_simulador.txt"),DateTime.UtcNow.ToString("o")+" Camera="+(c?c.transform.position.ToString("F5"):"null")+" Time="+Time.time+" Sim="+(sim?sim.manipulatingFPS.ToString():"null")+"\n");
        }
    }
    public static void RepairAndVerify(){
        foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.EndsWith(".inputactions")))
            AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        foreach(var p in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Samples/XR Interaction Toolkit/")&&p.EndsWith(".prefab")))
            AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        Repair();Fase2Verification.Run();
    }
    public static void OpenForReview(){
        EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        var result=new System.Text.StringBuilder();result.AppendLine("UTC="+DateTime.UtcNow.ToString("o"));
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Fase2/Models/")&&p.EndsWith(".fbx"))){
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            result.AppendLine("MODEL "+path+" exists="+(model!=null));
            if(model)foreach(var mf in model.GetComponentsInChildren<MeshFilter>()){
                var m=mf.sharedMesh;result.AppendLine("MESH "+mf.name+" vertices="+m.vertexCount+" uv="+m.uv.Length+" normals="+m.normals.Length+" bounds="+m.bounds.size);
            }
        }
        var lab=UnityEngine.Object.FindFirstObjectByType<Fase2.EnergyLab>();int invalid=0;int count=0;
        foreach(var renderer in lab.GetComponentsInChildren<MeshRenderer>()){
            if(renderer.GetComponent<TMPro.TMP_SubMesh>())continue;
            if(renderer.GetComponent<TMPro.TMP_Text>())continue;
            foreach(var m in renderer.sharedMaterials){count++;if(!m||!m.shader||!m.HasProperty("_BaseMap")||!m.GetTexture("_BaseMap")){invalid++;result.AppendLine("INVALID MATERIAL "+renderer.name);}}
        }
        result.AppendLine("MaterialSlots="+count+" invalid="+invalid);
        File.WriteAllText(Path.Combine(Fase2Audit.Output,"Assets_importados.txt"),result.ToString());
        EditorApplication.isPaused=false;EditorApplication.isPlaying=true;
    }
    static void Repair()
    {
        EditorApplication.isPaused=false;
        var paths=AssetDatabase.GetAllAssetPaths();
        foreach(var p in paths.Where(p=>p.EndsWith(".shadersubgraph")).Concat(paths.Where(p=>p.EndsWith(".shadergraph")||p.EndsWith(".urtshader"))))
            AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();Debug.Log("FASE2_REIMPORT_SHADERS_DONE");
        File.WriteAllText(Path.Combine(Fase2Audit.Output,"Reimportacion.txt"),"Se reimportaron shadergraph y urtshader sin cambiar las versiones de paquetes. "+DateTime.UtcNow.ToString("o"));
    }
}
