using UnityEngine;
using HoloDebut.Runtime;
namespace HoloDebut.Presentation
{
    public sealed class DevelopmentOverlay : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameSessionController game;
        private bool visible;
        private GUIStyle style;
        private void Awake(){game=GetComponent<GameSessionController>();}
        private void Update(){if(Input.GetKeyDown(KeyCode.F3))visible=!visible;}
        private void OnGUI()
        {
            if(!visible||!game||game.Model==null||!game.Dice)return;
            if(style==null)style=new GUIStyle(GUI.skin.box){font=JapaneseFont.Load()};
            var m=game.Model;string text="開発情報（F3で非表示） | セッション "+m.SessionId+" / 手番 "+m.Turns.CompletedTurns+" / 投球 "+m.ActiveRoll.RollId+" / 初期シード "+game.Dice.LaunchSeed;
            foreach(var d in game.Dice.Dice)if(d.gameObject.activeSelf&&d.Body)
                text+="\n"+d.name+" 速度="+d.Body.linearVelocity.magnitude.ToString("F4")+" 角速度="+d.Body.angularVelocity.magnitude.ToString("F3")+" 支持="+d.HasSupport+" 静止="+d.StillSeconds.ToString("F2")+" 上面内積="+d.BestDot.ToString("F3")+" / "+d.SecondDot.ToString("F3");
            GUI.Box(new Rect(Screen.width*0.31f,8,Screen.width*0.68f,100),text,style);
        }
#endif
    }
}
