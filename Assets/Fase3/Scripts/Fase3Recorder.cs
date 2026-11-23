#if UNITY_EDITOR
using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using Unity.Collections;

// Editor-only capture of actual camera frames and the Unity audioWriter mixer. No fabricated frames.
/// <summary>Captures rendered camera frames and the Unity audio mixer during Editor playback.</summary>
public sealed class Fase3Recorder : MonoBehaviour
{
    Process encoder; RenderTexture target;Texture2D pixels;BinaryWriter audioWriter;
    bool recording;int frames,channels;float priorDelta;string folder;
    void Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../F3Trabajo/Video"));Directory.CreateDirectory(folder);
        string ffmpeg=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Fase 2 (Construcción del entorno de realidad virtual)/Desarrollo/Edicion_video/ffmpeg.exe"));
        priorDelta=Time.captureDeltaTime;Time.captureFramerate=30;
        channels=AudioSettings.speakerMode==AudioSpeakerMode.Mono?1:2;
        if(AudioSettings.speakerMode!=AudioSpeakerMode.Mono&&AudioSettings.speakerMode!=AudioSpeakerMode.Stereo)throw new InvalidOperationException("La captura requiere mezcla mono o estéreo.");
        target=new RenderTexture(1920,1080,24);pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var info=new ProcessStartInfo(ffmpeg,"-y -f rawvideo -pixel_format rgb24 -video_size 1920x1080 -framerate 30 -i pipe:0 -vf vflip -an -c:v libx264 -preset veryfast -crf 19 -pix_fmt yuv420p \""+Path.Combine(folder,"Captura_simulador_sin_audio.mp4")+"\"") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
        encoder=Process.Start(info);encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(folder,"Codificacion.log"),e.Data+"\n");};encoder.BeginErrorReadLine();
        audioWriter=new BinaryWriter(File.Create(Path.Combine(folder,"Audio_juego.f32")));
        recording=AudioRenderer.Start();if(!recording)throw new InvalidOperationException("No se pudo iniciar AudioRenderer.");
        File.WriteAllText(Path.Combine(folder,"Captura_metadatos.txt"),"Unity="+Application.unityVersion+"\nFPS de captura=30 (no medicion de rendimiento)\nResolucion=1920x1080\nAudioRate="+AudioSettings.outputSampleRate+"\nCanales="+channels+"\nOrigen=Camera.main y mezcla real de AudioRenderer\nControles y poses del simulador automatizados; no prueba manual ni visor físico.\n");
    }
    void LateUpdate()
    {
        if(!recording||!Camera.main)return;
        var cam=Camera.main;var old=cam.targetTexture;var active=RenderTexture.active;
        try {
            cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false);
            var bytes=pixels.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
            int samples=AudioRenderer.GetSampleCountForCaptureFrame();
            using(var data=new NativeArray<float>(samples*channels,Allocator.Temp)) {if(!AudioRenderer.Render(data))throw new InvalidOperationException("Fallo al capturar mezcla de audioWriter");foreach(float value in data)audioWriter.Write(value);}
            frames++;
        } finally {cam.targetTexture=old;RenderTexture.active=active;}
    }
    void OnDestroy()
    {
        if(recording)AudioRenderer.Stop();recording=false;Time.captureDeltaTime=priorDelta;
        audioWriter?.Dispose();if(encoder!=null){encoder.StandardInput.Close();encoder.WaitForExit(10000);encoder.Dispose();}
        if(target){target.Release();DestroyImmediate(target);}if(pixels)DestroyImmediate(pixels);
        if(folder!=null)File.AppendAllText(Path.Combine(folder,"Captura_metadatos.txt"),"Frames="+frames+"\nDuracion="+(frames/30.0).ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n");
    }
}

#endif
