using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    public sealed class GameSpeedController : MonoBehaviour
    {
        private readonly ProgressInput progress=new ProgressInput();
        private float oldScale,oldFixed;private Vector3 oldGravity;
        private bool previousHeld,suppressUntilRelease;
        public bool PointerHeld {get;set;}
        public bool Fast => Time.timeScale>1;
        public System.Action Pressed;
        public void PointerDown()
        {
            PointerHeld=true;
            if(suppressUntilRelease||previousHeld)return;
            if(progress.Press(Time.realtimeSinceStartupAsDouble)){previousHeld=true;Pressed?.Invoke();}
        }
        public void PointerUp()
        {PointerHeld=false;if(!Input.GetKey(KeyCode.Space)){previousHeld=false;progress.Release();}}
        private void OnEnable()
        {oldScale=Time.timeScale;oldFixed=Time.fixedDeltaTime;oldGravity=Physics.gravity;Time.timeScale=1;Time.fixedDeltaTime=1f/120;Physics.gravity=new Vector3(0,-9.81f,0);}
        private void Update()
        {
            bool held=PointerHeld||Input.GetKey(KeyCode.Space);
            if(suppressUntilRelease){if(!held)suppressUntilRelease=false;return;}
            if(held&&!previousHeld&&progress.Press(Time.realtimeSinceStartupAsDouble))Pressed?.Invoke();
            if(!held)progress.Release();previousHeld=held;
            Time.timeScale=progress.FastForward(Time.realtimeSinceStartupAsDouble)?2:1;
            // fixedDeltaTime stays fixed in game time, including at 2x.
        }
        public void Clear()
        {PointerHeld=false;previousHeld=false;progress.Release();Time.timeScale=1;suppressUntilRelease=true;}
        private void OnApplicationFocus(bool focus){if(!focus)Clear();}
        private void OnDisable(){progress.Release();PointerHeld=false;Time.timeScale=oldScale;Time.fixedDeltaTime=oldFixed;Physics.gravity=oldGravity;}
    }
}
