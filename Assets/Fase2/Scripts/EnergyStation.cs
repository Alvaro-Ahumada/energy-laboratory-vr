using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Fase2
{
    /// <summary>Activates the original station after the collection goal.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class EnergyStation : MonoBehaviour
    {
        public EnergyLab lab;
        XRSimpleInteractable interactable;
        void Awake() { interactable = GetComponent<XRSimpleInteractable>(); }
        void OnEnable() { if (!interactable) interactable=GetComponent<XRSimpleInteractable>(); interactable.selectEntered.AddListener(Select); }
        void OnDisable() { if (interactable) interactable.selectEntered.RemoveListener(Select); }
        void Select(SelectEnterEventArgs args) { if (lab) lab.TryActivate(); }
    }
}
