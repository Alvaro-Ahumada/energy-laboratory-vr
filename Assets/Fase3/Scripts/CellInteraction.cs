using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Fase3
{
    /// <summary>Handles XR hover, grip, cell recovery and feedback on a successful connection.</summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class CellInteraction : MonoBehaviour
    {
        public int id;
        public LabSession session;
        public AudioSource audioSource;
        public ParticleSystem halo;
        public string grabInstruction = "Mantén el agarre, transporta la celda y suéltala en un receptor.";
        public bool Accepted { get; private set; }
        public XRGrabInteractable Grab { get; private set; }
        public Transform AcceptedAnchor { get; set; }
        Vector3 home; Quaternion rotation; Rigidbody body;
        Renderer[] renderers; MaterialPropertyBlock properties;
        void Awake()
        {
            Grab=GetComponent<XRGrabInteractable>(); body=GetComponent<Rigidbody>();
            home=transform.position; rotation=transform.rotation;
            renderers=GetComponentsInChildren<MeshRenderer>(); properties=new MaterialPropertyBlock();
        }
        void OnEnable()
        {
            if(!Grab) Grab=GetComponent<XRGrabInteractable>();
            Grab.hoverEntered.AddListener(Hover); Grab.hoverExited.AddListener(Unhover);
            Grab.selectEntered.AddListener(Selected); Grab.selectExited.AddListener(Released);
        }
        void OnDisable()
        {
            if(!Grab) return;
            Grab.hoverEntered.RemoveListener(Hover); Grab.hoverExited.RemoveListener(Unhover);
            Grab.selectEntered.RemoveListener(Selected); Grab.selectExited.RemoveListener(Released);
        }
        void Hover(HoverEnterEventArgs a)
        {
            if(Accepted||session.State!=LabState.Playing||a.interactorObject is XRSocketInteractor) return;
            Tint(new Color(.55f,1,1)); session.SetHint("CELDA " + id + " | " + grabInstruction);
            Debug.Log("F3_HOVER cell="+id);
        }
        void Unhover(HoverExitEventArgs a) { if (!Accepted && !Grab.isSelected) { Tint(Color.white); session.ClearHint(); } }
        /// <summary>Explains where to take the cell after a controller grip.</summary>
        void Selected(SelectEnterEventArgs a)
        {
            if(a.interactorObject is XRSocketInteractor) return;
            session.SetHint("CELDA " + id + " EN MANO | Llévala a la estación y suéltala sobre un receptor libre.");
            if(audioSource) audioSource.Play(); Debug.Log("F3_GRAB cell="+id);
        }
        /// <summary>Reports release without awarding a point.</summary>
        void Released(SelectExitEventArgs a)
        {
            if(a.interactorObject is XRSocketInteractor || Accepted) return;
            session.SetHint("Celda liberada. El contador cambia al conectarla."); Debug.Log("F3_RELEASE cell="+id);
        }
        void Update()
        {
            if(Accepted && AcceptedAnchor && Grab.enabled)
            {
                SetAvailable(false);
                transform.SetPositionAndRotation(AcceptedAnchor.position,AcceptedAnchor.rotation);
                body.position=transform.position;body.rotation=transform.rotation;
            }
            if(!Accepted && session.State==LabState.Playing && !Grab.isSelected &&
                (transform.position.y < -.4f || Mathf.Abs(transform.position.x)>6 || transform.position.z < -3 || transform.position.z >9))
             // Modified: Alvaro Ahumada personalized the recovery message with AI assistance.
            { RestorePose();session.SetHint("La celda regresó a su pedestal. Puedes intentarlo de nuevo.");Debug.Log("F3_RECOVER cell="+id); }
        }
        /// <summary>Locks the connected cell and emits its visual confirmation.</summary>
        public void MarkAccepted()
        {
            Accepted=true; Tint(new Color(.4f,1,.65f));
            if(halo) { halo.Stop(true,ParticleSystemStopBehavior.StopEmitting);halo.Emit(20); }
        }
        /// <summary>Enables or disables grabbing and adjusts the rigidbody accordingly.</summary>
        public void SetAvailable(bool value)
        {
            if(!Grab) Grab=GetComponent<XRGrabInteractable>();
            Grab.enabled=value;
            if(body) { body.isKinematic=!value; if(!body.isKinematic) { body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero; } }
        }
        /// <summary>Restores the original pose and clears the previous connection.</summary>
        public void ResetCell()
        {
            SetAvailable(false); Accepted=false; AcceptedAnchor=null; RestorePose(); Tint(Color.white);
            if(audioSource) audioSource.Stop(); if(halo) { halo.Clear();halo.Play(); }
            SetAvailable(true);
        }
        void RestorePose()
        {
            if(!body) return;
            bool k=body.isKinematic;body.isKinematic=true;body.position=home;body.rotation=rotation;
            transform.SetPositionAndRotation(home,rotation);body.isKinematic=k;
            if(!k) { body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero; }
        }
        void Tint(Color color) { if(renderers==null) return; foreach(var r in renderers) { r.GetPropertyBlock(properties);properties.SetColor("_BaseColor",color);r.SetPropertyBlock(properties); } }
    }
}
