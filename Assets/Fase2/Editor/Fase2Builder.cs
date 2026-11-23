using System;
using System.IO;
using System.Linq;
using Fase2;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>Assembles the scene and its assets for Fase2Builder.</summary>
public static class Fase2Builder
{
    const string A="Assets/Fase2/";
    static Material panel, circuit, floor, particle;
    static Transform root;
    [MenuItem("Fase 2/Construir laboratorio en DemoScene")]
    /// <summary>Builds the corresponding project revision in Unity.</summary>
    public static void Build()
    {
        var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        if (scene.GetRootGameObjects().Any(g=>g.name=="Fase2_Laboratorio"))
            throw new InvalidOperationException("El laboratorio ya existe. Edite la escena o restaure el respaldo antes de reconstruir.");
        Directory.CreateDirectory(Fase2Audit.Output);
        var backup=Path.Combine(Fase2Audit.Output,"DemoScene_previa.unity.backup");
        if (!File.Exists(backup)) File.Copy(Fase2Audit.ScenePath,backup);
        File.Copy(Fase2Audit.ScenePath+".meta",backup+".meta",true);
        AssetDatabase.Refresh();
        panel=Mat("Panel","Panel_Atlas.png",.4f,.42f);
        circuit=Mat("Circuito","Circuito.png",.2f,.55f,true);
        floor=Mat("Suelo","Suelo.png",.25f,.3f);
        particle=AssetDatabase.LoadAssetAtPath<Material>(A+"Materials/Particulas.mat") ?? new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        particle.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"Textures/Particula.png"));
        particle.SetColor("_BaseColor",new Color(.3f,1f,.8f,1));particle.SetFloat("_Surface",1);particle.SetFloat("_Blend",0);
        particle.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);particle.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);particle.SetInt("_ZWrite",0);
        particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particle.renderQueue=3000;
        if(!AssetDatabase.Contains(particle))AssetDatabase.CreateAsset(particle,A+"Materials/Particulas.mat");
        foreach(var path in AssetDatabase.FindAssets("t:Model",new[]{A+"Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            imp.importAnimation=false;imp.isReadable=true;imp.SaveAndReimport();
        }
        // Group inherited objects while keeping the XR rig and managers active.
        var old=scene.GetRootGameObjects();var legacy=new GameObject("Fase1_Muestras_Conservadas");
        foreach(var g in old)
        {
            if(g.name.Contains("XR Origin") || g.name=="XR Interaction Manager" || g.name=="EventSystem") continue;
            g.transform.SetParent(legacy.transform,true);
        }
        legacy.SetActive(false);
        root=new GameObject("Fase2_Laboratorio").transform;
        var lab=root.gameObject.AddComponent<EnergyLab>();
        foreach(var g in scene.GetRootGameObjects().Where(g=>g.name.Contains("XR Origin")))
        { g.transform.position=new Vector3(0,.03f,-1);g.transform.rotation=Quaternion.identity; }
        var geo=new GameObject("Arquitectura").transform;geo.SetParent(root);
        for(int x=-5;x<=5;x+=2)for(int z=-2;z<=8;z+=2)Model("ModuloSuelo",new Vector3(x,0,z),Quaternion.identity,geo);
        // Use one collision surface across floor seams and preserve XR locomotion.
        var ground=new GameObject("Suelo_Navegable");ground.transform.SetParent(root);
        var gc=ground.AddComponent<BoxCollider>();gc.center=new Vector3(0,-.075f,3);gc.size=new Vector3(12,.15f,12);
        var ta=ground.AddComponent<TeleportationArea>();ta.colliders.Add(gc);ta.interactionLayers=1<<31;
        for(int x=-5;x<=5;x+=2)Wall(new Vector3(x,0,9),Quaternion.identity,geo);
        for(int z=-2;z<=8;z+=2){Wall(new Vector3(-6,0,z),Quaternion.Euler(0,90,0),geo);Wall(new Vector3(6,0,z),Quaternion.Euler(0,-90,0),geo);}
        // The original entrance has a low boundary to mark the edge of the floor.
        var entry=new GameObject("Limite_Entrada");entry.transform.SetParent(root);var ec=entry.AddComponent<BoxCollider>();ec.center=new Vector3(0,.5f,-3);ec.size=new Vector3(12,1,.15f);
        var entryVisual=Model("ModuloMuro",new Vector3(0,0,-3),Quaternion.identity,geo);entryVisual.transform.localScale=new Vector3(6,.2f,1);
        var music=new GameObject("Musica_Ambiente");music.transform.SetParent(root);var ma=music.AddComponent<AudioSource>();ma.clip=Clip("Ambiente_16s.wav");ma.loop=true;ma.volume=.35f;ma.spatialBlend=0;ma.playOnAwake=true;
        var station=Model("Estacion",new Vector3(0,0,8),Quaternion.identity,root);station.name="Estacion_Activacion";
        var sc=station.AddComponent<BoxCollider>();sc.center=new Vector3(0,.65f,0);sc.size=new Vector3(1.6f,1.3f,.95f);
        var si=station.AddComponent<XRSimpleInteractable>();si.colliders.Add(sc);station.AddComponent<EnergyStation>().lab=lab;
        lab.stationAudio=SpatialAudio(station,"Activacion.wav");lab.successClip=Clip("Activacion.wav");lab.waitingClip=Clip("Espera.wav");
        var accent=new GameObject("Luz_Estacion");accent.transform.SetParent(root);accent.transform.position=new Vector3(0,2,7.5f);lab.stationLight=accent.AddComponent<Light>();lab.stationLight.type=LightType.Point;lab.stationLight.color=new Color(.2f,.8f,1);lab.stationLight.intensity=1;lab.stationLight.range=4;lab.stationLight.shadows=LightShadows.None;
        lab.progress=Text("Progreso",new Vector3(0,2.25f,8),"ENERGÍA: 0 / 3\nSelecciona las tres celdas",.15f,new Vector2(4,1));
        Text("Titulo",new Vector3(0,2.8f,8.65f),"LABORATORIO DE ENERGÍA",.19f,new Vector2(7,.6f));
        Text("Instrucciones",new Vector3(0,1.7f,.8f),"EXPLORA Y CONECTA\nSelecciona 3 celdas con el controlador.\nDespués selecciona la estación del fondo.",.095f,new Vector2(4,1));
        Vector3[] points={new Vector3(-3,0,2),new Vector3(3,0,2),new Vector3(0,0,5)};
        for(int i=0;i<3;i++)
        {
            var pedestal=Model("Pedestal",points[i],Quaternion.identity,root);pedestal.name="Pedestal_0"+(i+1);
            var pc=pedestal.AddComponent<BoxCollider>();pc.center=new Vector3(0,.5f,0);pc.size=new Vector3(1.2f,1,1.2f);
            var cell=new GameObject("Celda_0"+(i+1));cell.transform.SetParent(root);cell.transform.position=points[i]+Vector3.up*1.08f;
            var visual=Model("CeldaEnergia",cell.transform.position,Quaternion.identity,cell.transform);
            var cc=cell.AddComponent<BoxCollider>();cc.center=new Vector3(0,.22f,0);cc.size=new Vector3(.38f,.48f,.38f);
            var interaction=cell.AddComponent<XRSimpleInteractable>();interaction.colliders.Add(cc);
            var logic=cell.AddComponent<EnergyCell>();logic.cellId=i+1;logic.lab=lab;logic.visual=visual;logic.audioSource=SpatialAudio(cell,"Celda.wav");
            var fx=new GameObject("Halo_Recolectable");fx.transform.SetParent(cell.transform,false);fx.transform.localPosition=new Vector3(0,.2f,0);
            var ps=fx.AddComponent<ParticleSystem>();ps.Stop();var main=ps.main;main.startLifetime=1.2f;main.startSpeed=.12f;main.startSize=.07f;main.startColor=new Color(.25f,1,.8f,.8f);main.maxParticles=60;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=14;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.27f;
            var col=ps.colorOverLifetime;col.enabled=true;var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.1f,1,.7f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});col.color=grad;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=particle;logic.halo=ps;ps.Play();
            PrefabUtility.SaveAsPrefabAsset(fx,A+"VFX/Halo_0"+(i+1)+".prefab");
            // Assign the scene reference on each instance rather than the prefab.
            var targetLab=logic.lab;logic.lab=null;PrefabUtility.SaveAsPrefabAsset(cell,A+"Prefabs/Celda_0"+(i+1)+".prefab");logic.lab=targetLab;
            Text("Etiqueta_"+(i+1),points[i]+new Vector3(0,.65f,-.63f),"CELDA 0"+(i+1),.08f,new Vector2(1,.25f));
        }
        var sun=new GameObject("Luz_Principal");sun.transform.SetParent(root);sun.transform.rotation=Quaternion.Euler(45,-35,0);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.color=new Color(1,.91f,.78f);light.shadows=LightShadows.Soft;RenderSettings.sun=light;
        var sky=AssetDatabase.LoadAssetAtPath<Material>(A+"Materials/Cielo.mat") ?? new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.26f,.47f,.59f));sky.SetColor("_GroundColor",new Color(.1f,.17f,.22f));sky.SetFloat("_Exposure",1.1f);if(!AssetDatabase.Contains(sky))AssetDatabase.CreateAsset(sky,A+"Materials/Cielo.mat");RenderSettings.skybox=sky;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.6f,.68f);RenderSettings.ambientEquatorColor=new Color(.32f,.43f,.49f);RenderSettings.ambientGroundColor=new Color(.19f,.24f,.28f);RenderSettings.fog=false;
        // Keep the inherited scene path and GUID as the only enabled scene.
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Fase2Audit.ScenePath,true)};
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Fase2Audit.Output,"Construccion.txt"),"Laboratorio guardado en "+scene.path+"\nUTC="+DateTime.UtcNow.ToString("o")+"\nOriginal conservado en respaldo.\n");
        Debug.Log("FASE2_BUILD_OK");
    }
    static Material Mat(string name,string texture,float metallic,float smooth,bool emissive=false)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>(A+"Materials/"+name+".mat") ?? new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(A+"Textures/"+texture));m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
        if(emissive){m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));m.SetColor("_EmissionColor",Color.white*.5f);}
        if(!AssetDatabase.Contains(m))AssetDatabase.CreateAsset(m,A+"Materials/"+name+".mat");return m;
    }
    static GameObject Model(string name,Vector3 pos,Quaternion rot,Transform parent)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(A+"Models/"+name+".fbx");if(!prefab)throw new Exception("Modelo no importado: "+name);
        var g=new GameObject(name);g.transform.SetParent(parent);g.transform.position=pos;g.transform.rotation=rot;
        var mesh=(GameObject)PrefabUtility.InstantiatePrefab(prefab);mesh.transform.SetParent(g.transform,false);
        foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m!=null&&m.name.StartsWith("Circuito")?circuit:m!=null&&m.name.StartsWith("Suelo")?floor:panel).ToArray();
        return g;
    }
    static void Wall(Vector3 pos,Quaternion rot,Transform parent){var g=Model("ModuloMuro",pos,rot,parent);var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,1.5f,0);c.size=new Vector3(2,3,.3f);}
    static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(A+"Audio/"+name);
    static AudioSource SpatialAudio(GameObject g,string file){var a=g.AddComponent<AudioSource>();a.clip=Clip(file);a.playOnAwake=false;a.spatialBlend=1;a.volume=.65f;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=1;a.maxDistance=10;a.dopplerLevel=0;return a;}
    static TMP_Text Text(string name,Vector3 pos,string text,float size,Vector2 rect)
    {
        var go=new GameObject(name);go.transform.SetParent(root);go.transform.position=pos;
        var tmp=go.AddComponent<TextMeshPro>();tmp.text=text;tmp.fontSize=size*10;tmp.alignment=TextAlignmentOptions.Center;tmp.color=new Color(.8f,.98f,.94f);tmp.rectTransform.sizeDelta=rect;return tmp;
    }
}
