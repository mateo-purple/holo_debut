using UnityEngine;
using HoloDebut.Runtime;
namespace HoloDebut.Presentation
{
    public sealed class DevelopmentOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameSessionController game;
        private void Awake(){game=GetComponent<GameSessionController>();}
        private void OnGUI()
        {
            if(!game||game.Model==null||!game.Dice)return;
            var m=game.Model;string text="DEV | session "+m.SessionId+" / turn "+m.Turns.CompletedTurns+" / roll "+m.ActiveRoll.RollId+" / launch seed "+game.Dice.LaunchSeed;
            foreach(var d in game.Dice.Dice)if(d.gameObject.activeSelf&&d.Body)
                text+="\n"+d.name+" v="+d.Body.linearVelocity.magnitude.ToString("F4")+" w="+d.Body.angularVelocity.magnitude.ToString("F3")+" supported="+d.HasSupport+" still="+d.StillSeconds.ToString("F2")+" dot="+d.BestDot.ToString("F3")+" / "+d.SecondDot.ToString("F3");
            GUI.Box(new Rect(Screen.width*0.31f,8,Screen.width*0.68f,100),text);
        }
#endif
    }
}
