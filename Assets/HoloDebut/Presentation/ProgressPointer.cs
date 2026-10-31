using UnityEngine;
using UnityEngine.EventSystems;
using HoloDebut.Runtime;
namespace HoloDebut.Presentation
{
    public sealed class ProgressPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public GameSpeedController speed;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&speed)speed.PointerDown();}
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&speed)speed.PointerUp();}
        private void OnDisable(){if(speed)speed.PointerHeld=false;}
    }
}
