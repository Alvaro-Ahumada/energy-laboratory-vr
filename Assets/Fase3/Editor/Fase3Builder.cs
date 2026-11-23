using System;
using System.IO;
using System.Linq;
using Fase3;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Assembles the scene and its assets for Fase3Builder.</summary>
public static class Fase3Builder
{
    static Transform root; static LabSession game; static Sprite backdrop;
    [MenuItem("Fase 3/Construir UI e interacciones")]
    /// <summary>Builds the corresponding project revision in Unity.</summary>
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de construir.");
        var scene=EditorSceneManager.OpenScene(Fase2Audit.ScenePath);
        if(UnityEngine.Object.FindFirstObjectByType<LabSession>()) throw new InvalidOperationException("La Fase 3 ya está construida; no duplicar gestores.");
        var old=UnityEngine.Object.FindFirstObjectByType<Fase2.EnergyLab>();
        root=new GameObject("Fase3_Interacciones_UI").transform;game=root.gameObject.AddComponent<LabSession>();
        game.stationAudio=old.stationAudio;game.successSound=old.successClip;game.waitingSound=old.waitingClip;game.stationLight=old.stationLight;
        old.enabled=false;
        foreach(var t in old.GetComponentsInChildren<TMP_Text>()) if(t.name=="Progreso"||t.name=="Instrucciones")t.gameObject.SetActive(false);
        var oldStation=UnityEngine.Object.FindFirstObjectByType<Fase2.EnergyStation>();
        oldStation.enabled=false; oldStation.GetComponent<XRSimpleInteractable>().enabled=false;
        var cells=UnityEngine.Object.FindObjectsByType<Fase2.EnergyCell>(FindObjectsSortMode.None).OrderBy(x=>x.cellId).ToArray();
        game.cells=new CellInteraction[cells.Length];
        for(int i=0;i<cells.Length;i++)
        {
            var c=cells[i];c.enabled=false;c.GetComponent<XRSimpleInteractable>().enabled=false;
            var body=c.gameObject.AddComponent<Rigidbody>();body.mass=.35f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            var grab=c.gameObject.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;grab.useDynamicAttach=true;
            var logic=c.gameObject.AddComponent<CellInteraction>();logic.id=c.cellId;logic.session=game;logic.audioSource=c.audioSource;logic.halo=c.halo;
            game.cells[i]=logic;
            // Old component kept disabled as provenance; its selection listener is not active.
        }
        game.sockets=new EnergySocket[3];
        for(int i=0;i<3;i++)
        {
            var g=new GameObject("Receptor_"+(i+1));g.transform.SetParent(root);g.transform.position=new Vector3((i-1)*.85f,1.25f,6.7f);
            var col=g.AddComponent<SphereCollider>();col.isTrigger=true;col.radius=.32f;
            var s=g.AddComponent<EnergySocket>();s.session=game;s.showInteractableHoverMeshes=false;s.recycleDelayTime=.1f;
            var attach=new GameObject("Punto_encaje").transform;attach.SetParent(g.transform,false);s.attachTransform=attach;
            game.sockets[i]=s;
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);plate.name="Base_receptor";plate.transform.SetParent(g.transform,false);plate.transform.localPosition=new Vector3(0,-.065f,0);plate.transform.localScale=new Vector3(.62f,.045f,.62f);
            plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Fase2/Materials/Circuito.mat");
            Label(g.transform,"Rótulo","CONECTAR " +(i+1),new Vector3(0,.6f,0),new Vector2(1,.3f),.65f);
        }
        foreach(var b in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(b.GetType().Name=="InteractionAttachController") {var so=new SerializedObject(b);so.FindProperty("m_UseDistanceBasedVelocityScaling").boolValue=false;so.FindProperty("m_UseMomentum").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
            if(b.GetType().Name=="ControllerInputActionManager") {var so=new SerializedObject(b);so.FindProperty("m_SmoothMotionEnabled").boolValue=false;so.FindProperty("m_SmoothTurnEnabled").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
            if(b.GetType().Name.Contains("ContinuousMoveProvider")||b.GetType().Name.Contains("ContinuousTurnProvider"))b.enabled=false;
        }
        var importer=AssetImporter.GetAtPath("Assets/Fase3/UI/Panel_UI.png") as TextureImporter;
        if(importer!=null){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();}
        backdrop=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Fase3/UI/Panel_UI.png");
        game.menu=Panel("Canvas_Menu",new Vector3(0,1.75f,1.4f));
        Text(game.menu.transform,"Título","LABORATORIO DE ENERGÍA",0,210,34);
        Text(game.menu.transform,"Subtítulo","CONEXIÓN XR  /  FASE 3",0,153,22);
        Text(game.menu.transform,"Objetivo","Transporta las tres celdas a la estación.\nTienes 90 segundos para conectarlas.",0,78,23);
        Button(game.menu.transform,"Iniciar juego",0,-22,game.Begin);
        Button(game.menu.transform,"Créditos",0,-108,game.ShowCredits);
        Button(game.menu.transform,"Salir",0,-194,game.ExitApplication);
        game.credits=Panel("Vista_Creditos",new Vector3(0,1.75f,1.4f),false);
        Text(game.credits.transform,"Título","CRÉDITOS",0,205,34);
        Text(game.credits.transform,"Datos","Alvaro Daniel Ahumada Navarro\nIngeniería Multimedia · UNAD\nRealidad Virtual 213043 · Grupo 1\n\nAssets de Fase 2 y Unity XR Toolkit.\nProgramación y documentación con asistencia de IA.",0,40,22,400);
        Button(game.credits.transform,"Volver al menú",0,-194,game.CloseCredits);
        game.hud=Panel("Canvas_HUD",new Vector3(0,2.6f,4));
        Text(game.hud.transform,"Título","CONEXIÓN DE CELDAS",0,180,30);
        game.counter=Text(game.hud.transform,"Contador","CELDAS 0 / 3",-170,100,27,300);
        game.timer=Text(game.hud.transform,"Temporizador","TIEMPO 01:30",170,100,27,300);
        game.hint=Text(game.hud.transform,"Ayuda","Agarra y conecta las celdas.",0,-20,24,680);
        Button(game.hud.transform,"Volver al menú",0,-180,game.ReturnToMenu);
        game.win=Panel("Canvas_YouWin",new Vector3(0,1.75f,1.4f));
        Text(game.win.transform,"Título","YOU WIN",0,190,42);
        Text(game.win.transform,"Resultado","ESTACIÓN ACTIVADA\nLas tres celdas están conectadas.",0,75,28);
        Button(game.win.transform,"Reiniciar",0,-75,game.Begin);Button(game.win.transform,"Volver al menú",0,-175,game.ReturnToMenu);
        game.lose=Panel("Canvas_GameOver",new Vector3(0,1.75f,1.4f));
        Text(game.lose.transform,"Título","GAME OVER",0,190,42);
        Text(game.lose.transform,"Resultado","TIEMPO AGOTADO\nConecta las tres celdas antes del límite.",0,75,27);
        Button(game.lose.transform,"Reiniciar",0,-75,game.Begin);Button(game.lose.transform,"Volver al menú",0,-175,game.ReturnToMenu);
        game.credits.SetActive(false);game.hud.SetActive(false);game.win.SetActive(false);game.lose.SetActive(false);
        var uiModule=UnityEngine.Object.FindFirstObjectByType<XRUIInputModule>();
        if(!uiModule) throw new InvalidOperationException("No se encontró XRUIInputModule heredado.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Fase3Audit.Output,"Construccion_Fase3.txt"),"UTC="+DateTime.UtcNow.ToString("o")+"\nScene="+scene.path+"\nCells="+game.cells.Length+"\nSockets="+game.sockets.Length+"\nCanvas="+root.GetComponentsInChildren<Canvas>(true).Length);
        Debug.Log("F3_BUILD_OK");
    }
    static GameObject Panel(string name,Vector3 pos,bool canvas=true)
    {
        var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(root);g.transform.position=pos;g.transform.localScale=Vector3.one*.0032f;
        var rect=(RectTransform)g.transform;rect.sizeDelta=new Vector2(800,540);
        // Credits is an additional view with its own raycastable canvas; the four required canvases remain distinct.
        var c=g.AddComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;c.worldCamera=Camera.main;
        g.AddComponent<CanvasScaler>().dynamicPixelsPerUnit=10;g.AddComponent<TrackedDeviceGraphicRaycaster>();
        var image=g.AddComponent<Image>();image.sprite=backdrop;image.color=Color.white;
        return g;
    }
    static TMP_Text Text(Transform p,string name,string value,float x,float y,float size,float width=740)
    {
        var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,140);
        var t=g.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.color=new Color(.85f,1,1);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    static void Button(Transform p,string label,float x,float y,UnityAction action)
    {
        var g=new GameObject(label,typeof(RectTransform));g.transform.SetParent(p,false);var r=(RectTransform)g.transform;r.sizeDelta=new Vector2(430,68);r.anchoredPosition=new Vector2(x,y);
        var image=g.AddComponent<Image>();image.color=new Color(.06f,.34f,.39f);var b=g.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(.4f,1,1);colors.selectedColor=new Color(.4f,.85f,.9f);colors.pressedColor=new Color(.2f,.65f,.7f);b.colors=colors;
        UnityEventTools.AddPersistentListener(b.onClick,action);Text(g.transform,"Texto",label,0,0,26,420);
    }
    static void Label(Transform p,string name,string value,Vector3 pos,Vector2 size,float font)
    {var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.localPosition=pos;var t=g.AddComponent<TextMeshPro>();t.text=value;t.fontSize=font;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=size;}
}
