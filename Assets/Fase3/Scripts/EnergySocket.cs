using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace Fase3
{
    /// <summary>Accepts a nearby cell once and anchors it to the receiving station.</summary>
    public sealed class EnergySocket : XRSocketInteractor
    {
        public LabSession session;
        public CellInteraction Occupant { get; private set; }
        /// <summary>Checks the round, occupancy and current distance before a socket selection.</summary>
        public override bool CanSelect(IXRSelectInteractable target)
        {
            var cell=target.transform.GetComponent<CellInteraction>();
            if(!cell) return false;
            if(Occupant==cell) return IsSelecting(target);
            // Recheck physical proximity: trigger target lists may still contain pre-reset overlaps.
            return session.State==LabState.Playing && Occupant==null && !cell.Accepted &&
                Vector3.Distance(cell.transform.position,attachTransform.position)<.5f && base.CanSelect(target);
        }
        public override bool CanHover(IXRHoverInteractable target)
        {
            var cell=target.transform.GetComponent<CellInteraction>();
            return cell && !cell.Accepted && session.State==LabState.Playing && Occupant==null &&
                Vector3.Distance(cell.transform.position,attachTransform.position)<.5f && base.CanHover(target);
        }
        /// <summary>Registers the cell when the socket accepts it, independently of the grip event.</summary>
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            var cell=args.interactableObject.transform.GetComponent<CellInteraction>();
            if(cell && session.Accept(cell)) { Occupant=cell;cell.AcceptedAnchor=attachTransform; Debug.Log("F3_SOCKET "+name+" cell="+cell.id); }
        }
        /// <summary>Cancels the previous selection and frees the receiver for another round.</summary>
        public void ResetSocket()
        {
            socketActive=false;
            if(interactionManager) interactionManager.CancelInteractorSelection((IXRSelectInteractor)this);
            Occupant=null;socketActive=true;
        }
    }
}
