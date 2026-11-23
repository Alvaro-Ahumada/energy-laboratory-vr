using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Fase3
{
    public enum LabState { Menu, Playing, Win, Lose, Credits }
    /// <summary>Coordinates the timed round, unique cell count and world-space screens.</summary>
    public sealed class LabSession : MonoBehaviour
    {
        [Min(1)] public float durationSeconds = 90;
        public GameObject menu, hud, win, lose, credits;
        public TMP_Text counter, timer, hint;
        public CellInteraction[] cells;
        public EnergySocket[] sockets;
        public AudioSource stationAudio;
        public AudioClip successSound, waitingSound;
        public Light stationLight;
        public LabState State { get; private set; } = LabState.Menu;
        public int Count => accepted.Count;
        public float Remaining => State == LabState.Playing ? Mathf.Max(0, (float)(deadline - Time.timeAsDouble)) : frozenTime;
        readonly HashSet<int> accepted = new HashSet<int>();
        double deadline;
        float frozenTime;
        int displayedCount = -1, displayedTotal = -1, displayedSeconds = -1;
        const string Objective = "Conecta las tres celdas: agárralas, transpórtalas y suéltalas en los receptores."; // Modified: Codex adjusted this instruction at Alvaro’s request.
        void Start() { ReturnToMenu(); }
        /// <summary>Updates the result panel relative to the current view.</summary>
        void LateUpdate()
        {
            if (State == LabState.Playing) return;
            var view = Camera.main;
            if (!view) return;
            var panel = (State == LabState.Menu ? menu : State == LabState.Credits ? credits : State == LabState.Win ? win : lose).transform;
            var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.01f) return;
            forward.Normalize();
            var offset = panel.position - view.transform.position;
            // Keep the panel still for pointing, then recenter when it leaves the viewing cone.
            if (Vector3.Angle(forward, offset) > 32f || offset.magnitude < 1.4f || offset.magnitude > 3.2f)
                panel.position = view.transform.position + forward * 2.4f;
            var facing = Vector3.ProjectOnPlane(panel.position - view.transform.position, Vector3.up);
            if (facing.sqrMagnitude > 0.01f) panel.rotation = Quaternion.LookRotation(facing);
        }
        void Update()
        {
            if (State == LabState.Playing && Time.timeAsDouble >= deadline) Finish(false);
            Refresh();
        }
        /// <summary>Resets the cells and starts a new timed round.</summary>
        public void Begin()
        {
            ResetObjects(); accepted.Clear(); deadline = Time.timeAsDouble + durationSeconds;
            frozenTime = durationSeconds; SetState(LabState.Playing); SetHint(Objective);
            Debug.Log("F3_BEGIN duration=" + durationSeconds);
        }
        /// <summary>Registers a new cell ID only while the round is active and before its deadline. A tie is a loss.</summary>
        public bool Accept(CellInteraction cell)
        {
            if (State != LabState.Playing || cell == null || cell.Accepted) return false;
            // A deadline tie is a loss; both registration and Update use this clock.
            if (Time.timeAsDouble >= deadline) { Finish(false); return false; }
            if (!accepted.Add(cell.id)) return false;
            cell.MarkAccepted(); SetHint("Celda conectada. " + Count + " de " + cells.Length + ".");
            Debug.Log("F3_ACCEPT id=" + cell.id + " count=" + Count);
            if (Count == cells.Length) Finish(true);
            else if (stationAudio && successSound) stationAudio.PlayOneShot(successSound, 0.35f);
            Refresh(); return true;
        }
        /// <summary>Freezes the clock, locks interactions and displays the round outcome.</summary>
        void Finish(bool success)
        {
            if (State != LabState.Playing) return;
            frozenTime = Mathf.Max(0, (float)(deadline - Time.timeAsDouble));
            SetState(success ? LabState.Win : LabState.Lose);
            foreach (var c in cells) if (!c.Accepted) c.SetAvailable(false);
            foreach (var s in sockets) s.socketActive = false;
            if (stationAudio) stationAudio.PlayOneShot(success ? successSound : waitingSound);
            if (stationLight) stationLight.color = success ? new Color(.25f,1,.65f) : new Color(1,.5f,.2f);
            Debug.Log("F3_END " + State + " count=" + Count + " time=" + frozenTime);
        }
        public void ReturnToMenu()
        {
            State = LabState.Menu; ResetObjects(); accepted.Clear(); frozenTime = durationSeconds;
            foreach (var c in cells) c.SetAvailable(false);
            foreach (var s in sockets) s.socketActive = false;
            SetState(LabState.Menu);
        }
        public void ShowCredits() { SetState(LabState.Credits); }
        public void CloseCredits() { SetState(LabState.Menu); }
        /// <summary>Ends the Editor play session or exits the Android player.</summary>
        public void ExitApplication()
        {
            ReturnToMenu();
            Debug.Log("F3_EXIT_REQUEST editor=" + Application.isEditor);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        void ResetObjects()
        {
            foreach (var s in sockets) s.ResetSocket();
            foreach (var c in cells) c.ResetCell();
            Physics.SyncTransforms();
            if (stationAudio) stationAudio.Stop();
            if (stationLight) { stationLight.color = new Color(.2f,.8f,1); stationLight.intensity = 1; }
        }
        void SetState(LabState state)
        {
            State=state;
            // World-space screens open in front of the current viewpoint, including after teleporting.
            if(state!=LabState.Playing && Camera.main)
            {
                var camera=Camera.main.transform;var forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;
                var panel=state==LabState.Menu?menu:state==LabState.Credits?credits:state==LabState.Win?win:lose;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized;
                panel.transform.SetPositionAndRotation(camera.position+forward*2.4f,Quaternion.LookRotation(forward));
            }
            menu.SetActive(state==LabState.Menu); credits.SetActive(state==LabState.Credits);
            hud.SetActive(state==LabState.Playing); win.SetActive(state==LabState.Win); lose.SetActive(state==LabState.Lose);
            Refresh(); Debug.Log("F3_STATE " + state);
        }
        public void SetHint(string message) { if (hint) hint.text=message; }
        public void ClearHint() { if(State==LabState.Playing) SetHint(Objective); }
        /// <summary>Updates HUD strings only when their displayed values change.</summary>
        void Refresh()
        {
            // Fase 4: regenerate visible strings only when their value changes.
            int total = cells == null ? 3 : cells.Length;
            if(counter && (displayedCount != Count || displayedTotal != total))
            { counter.text="CELDAS  " + Count + " / " + total; displayedCount=Count; displayedTotal=total; }
            int seconds=Mathf.CeilToInt(Remaining);
            if(timer && displayedSeconds != seconds)
            { timer.text="TIEMPO  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00"); displayedSeconds=seconds; }
        }
    }
}
