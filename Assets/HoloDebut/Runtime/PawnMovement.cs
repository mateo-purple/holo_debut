using System;
using System.Collections;
using UnityEngine;
namespace HoloDebut.Runtime
{
    public sealed class PawnMovement : MonoBehaviour
    {
        public IEnumerator MoveStep(Transform pawn,Vector3 destination,float seconds,Func<bool> active)
        {
            Vector3 from=pawn.position;float elapsed=0;
            while(elapsed<seconds)
            {
                if(!active())yield break;
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/seconds);
                pawn.position=Vector3.Lerp(from,destination,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*0.10f);yield return null;
            }
            if(active())pawn.position=destination;
        }
    }
}
