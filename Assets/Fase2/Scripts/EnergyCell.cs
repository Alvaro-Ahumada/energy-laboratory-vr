using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Fase2
{
    /// <summary>Handles selection of the original Phase 2 collectible.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class EnergyCell : MonoBehaviour
    {
        public int cellId;
        public EnergyLab lab;
        public GameObject visual;
        public ParticleSystem halo;
        public AudioSource audioSource;
        public bool Collected { get; private set; }
        XRSimpleInteractable interactable;
        void Awake() { interactable = GetComponent<XRSimpleInteractable>(); }
        void OnEnable() { if (!interactable) interactable = GetComponent<XRSimpleInteractable>(); interactable.selectEntered.AddListener(OnSelected); }
        void OnDisable() { if (interactable) interactable.selectEntered.RemoveListener(OnSelected); }
        void OnSelected(SelectEnterEventArgs args) { Collect(); }
        public bool Collect()
        {
            if (Collected || !lab || !lab.RegisterCell(cellId)) return false;
            Collected = true;
            if (visual) visual.SetActive(false);
            // Keep the object active until its audio and last particle burst finish.
            if (halo) { halo.Stop(true, ParticleSystemStopBehavior.StopEmitting); halo.Emit(22); }
            if (audioSource) audioSource.Play();
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            return true;
        }
    }
}
