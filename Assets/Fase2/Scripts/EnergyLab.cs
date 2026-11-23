using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Fase2
{
    /// <summary>Tracks the original Phase 2 collection task.</summary>
    public sealed class EnergyLab : MonoBehaviour
    {
        public int requiredCells = 3;
        public TMP_Text progress;
        public Light stationLight;
        public AudioSource stationAudio;
        public AudioClip successClip;
        public AudioClip waitingClip;
        readonly HashSet<int> collected = new HashSet<int>();
        public int Count => collected.Count;
        public bool Activated { get; private set; }
        public int ActivationCount { get; private set; }
        float nextNotice;
        void Start() { UpdateDisplay(); }
        public bool RegisterCell(int id)
        {
            if (Activated || !collected.Add(id)) return false;
            UpdateDisplay();
            Debug.Log("FASE2_CELL id=" + id + " count=" + Count);
            return true;
        }
        public bool TryActivate()
        {
            if (Activated) return false;
            if (Count < requiredCells)
            {
                if (Time.unscaledTime >= nextNotice)
                {
                    if (stationAudio && waitingClip) stationAudio.PlayOneShot(waitingClip);
                    nextNotice = Time.unscaledTime + .5f;
                }
                UpdateDisplay();
                return false;
            }
            Activated = true;
            ActivationCount++;
            if (stationLight) { stationLight.color = new Color(.25f,1f,.65f); stationLight.intensity = 2f; }
            if (stationAudio && successClip) stationAudio.PlayOneShot(successClip);
            UpdateDisplay();
            Debug.Log("FASE2_ACTIVATED count=" + ActivationCount);
            return true;
        }
        void UpdateDisplay()
        {
            if (progress) progress.text = Activated ? "SISTEMA ACTIVADO\n3 / 3 celdas conectadas" :
                "ENERGÍA: " + Count + " / " + requiredCells + "\n" +
                (Count == requiredCells ? "Selecciona la estación" : "Selecciona las tres celdas");
        }
    }
}
