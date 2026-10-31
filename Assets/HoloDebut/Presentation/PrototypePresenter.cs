using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HoloDebut.Domain;
using HoloDebut.Runtime;

namespace HoloDebut.Presentation
{
    // Data is read only for display. Buttons submit commands to the session authority.
    public sealed class PrototypePresenter : MonoBehaviour
    {
        private GameSessionController game;
        private Font font;
        private Text header,status,players,enduranceInfo,finalInfo,itemInfo;
        private GameObject canvas,planPanel,endurancePanel,finalPanel;
        private Button item,retry,finish;
        private Text itemCaption;
        public void Initialize(GameSessionController controller)
        {
            game=controller;font=JapaneseFont.Load();
            canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false);canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=0.5f;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(transform,false);
            events.GetComponent<EventSystem>().sendNavigationEvents=false; // Space belongs only to progress, never a selected item button.
            var panel=Panel(canvas.transform,"Dashboard",12,12,540,1056,new Color(0.04f,0.06f,0.10f,0.96f));
            Label(panel.transform,"ホロデビュー",18,10,500,42,30);
            header=Label(panel.transform,"",18,58,500,80,22);
            status=Label(panel.transform,"",18,142,500,100,21);
            itemInfo=Label(panel.transform,"",18,245,500,36,22);
            item=MakeButton(panel.transform,"ロケットニンジンを使う（移動＋5）",18,290,500,50,game.RequestUseItem);
            itemCaption=item.GetComponentInChildren<Text>();
            retry=MakeButton(panel.transform,"同じ投球を再試行",18,348,500,50,game.RequestRetry);
            planPanel=Panel(panel.transform,"PlanChoices",18,410,500,165,Color.clear);
            Label(planPanel.transform,"配信企画を選んでください",0,0,490,32,22);
            MakeButton(planPanel.transform,"通常のゲーム配信",0,40,490,50,game.RequestNormalStream);
            MakeButton(planPanel.transform,"耐久配信に挑戦",0,100,490,50,game.RequestEndurance);
            endurancePanel=Panel(panel.transform,"EnduranceStatus",18,410,500,300,Color.clear);
            enduranceInfo=Label(endurancePanel.transform,"",0,0,490,236,21);
            finish=MakeButton(endurancePanel.transform,"追加挑戦せず終了する",0,242,490,50,game.RequestFinishEndurance);
            players=Label(panel.transform,"",18,730,500,240,20);
            MakeButton(panel.transform,"最初からやり直す",18,985,500,50,game.RestartSession);
            var progress=MakeButton(canvas.transform,"サイコロを振る（Space）\n0.2秒長押しで2倍速",600,960,660,92,null);
            progress.gameObject.AddComponent<ProgressPointer>().speed=game.Speed;progress.transition=Selectable.Transition.ColorTint;
            finalPanel=Panel(canvas.transform,"FinalResult",580,24,1315,900,new Color(0.04f,0.06f,0.10f,0.97f));
            finalInfo=Label(finalPanel.transform,"",28,24,1250,840,27);
            Update();
        }
        private GameObject Panel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var o=new GameObject(name,typeof(RectTransform),typeof(Image));o.transform.SetParent(parent,false);Place(o.GetComponent<RectTransform>(),x,y,w,h);o.GetComponent<Image>().color=color;return o;}
        private void Place(RectTransform rt,float x,float y,float w,float h)
        {rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);}
        private Text Label(Transform parent,string value,float x,float y,float w,float h,int size)
        {
            var o=new GameObject("Text",typeof(RectTransform),typeof(Text));o.transform.SetParent(parent,false);Place(o.GetComponent<RectTransform>(),x,y,w,h);
            var t=o.GetComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=Color.white;t.alignment=TextAnchor.UpperLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=size;t.raycastTarget=false;return t;
        }
        private Button MakeButton(Transform parent,string value,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var o=Panel(parent,"Button",x,y,w,h,new Color(0.14f,0.24f,0.36f));var b=o.AddComponent<Button>();b.targetGraphic=o.GetComponent<Image>();
            var t=Label(o.transform,value,8,5,w-16,h-10,22);t.alignment=TextAnchor.MiddleCenter;
            if(action!=null)b.onClick.AddListener(action);return b;
        }
        private void Update()
        {
            if(!game||!canvas)return;
            var m=game.Model;status.text=game.FatalError!=null?"処理を停止しました。設定を確認するか、最初からやり直してください。\n詳細はUnityのConsoleに記録しました。":game.Message+"\n"+(game.Dice?game.Dice.Status:"");
            if(m==null){header.text="設定エラー";item.interactable=false;planPanel.SetActive(false);endurancePanel.SetActive(false);finalPanel.SetActive(false);retry.gameObject.SetActive(false);return;}
            header.text="第"+m.Turns.Week+"週 "+JapaneseText.Day(m.Turns.DayIndex)+" / "+(game.Speed.Fast?"2倍速":"通常速度")+"\n"+m.CurrentPlayer.DisplayName+"："+JapaneseText.Phase(m.Phase);
            if(m.CurrentPlayer.Arrived)header.text+="（ゴール後）";
            header.text+="\n"+(m.Phase==SessionPhase.RunningSubgame?"耐久：サイコロ1個":m.CurrentPlayer.Arrived||m.Turns.Weekend?"チンチロ：サイコロ3個":"丁半：サイコロ2個");
            itemInfo.text="ロケットニンジン ×"+m.Inventory.Count(m.CurrentPlayer);
            if(m.Phase==SessionPhase.Moving)status.text+="\n移動予定 "+m.PlannedSteps+"マス / 残り "+m.RemainingSteps+"マス";
            bool endurance=m.Phase==SessionPhase.RunningSubgame;
            itemCaption.text=endurance?"ニンジン使用（移動＋5・投球＋1）":"ロケットニンジンを使う（移動＋5）";
            item.interactable=!game.Busy&&game.FatalError==null&&(endurance?game.Endurance.Session.CanUseItem:m.CanUseItem(m.CurrentPlayer.PlayerId));
            if(!item.interactable)itemInfo.text+=" | "+(m.Inventory.Count(m.CurrentPlayer)==0?"所持なし":game.Busy?"処理中は使用不可":m.CurrentPlayer.Arrived?"ゴール後は使用不可":"使用済み・使用条件外");
            planPanel.SetActive(m.Phase==SessionPhase.AwaitPlan);endurancePanel.SetActive(endurance);
            if(endurance)
            {
                var e=game.Endurance.Session;
                enduranceInfo.text="耐久配信："+e.LoopSize+"マス / 現在位置 "+e.Position+"番\nゲーム技能 "+m.CurrentPlayer.GameSkill+"（開始時 "+e.StartingGameSkill+"）\nスタミナ "+m.CurrentPlayer.Stamina+"（開始時 "+e.StartingStamina+"）"+
                    "\n残り基本投球 "+e.BasicRollsRemaining+"回 / 追加 "+e.ExtraRollsRemaining+"回\n有効投球 "+e.UsedRolls+"回 / 脱出報酬 ＋"+e.CurrentOrNextSuccessBonus+"人"+
                    "\n獲得資金 "+e.FundsEarned+" / 技能経験値 "+e.ExperienceEarned+"\n出目 "+e.LastPhysicalFace+" / 移動量 "+e.LastMovement+"マス";
                finish.gameObject.SetActive(e.CanDecline&&!game.Busy);
            }
            retry.gameObject.SetActive(game.Dice&&game.Dice.ErrorPaused);
            players.text=string.Join("\n",m.Players.Select(p=>p.DisplayName+"  "+p.CurrentNodeId+"番 / 登録者 "+p.Subscribers+"人\nニンジン "+m.Inventory.Count(p)+"個 / 資金 "+p.Funds+(p.Arrived?" / "+p.ArrivalRank+"着":"")));
            finalPanel.SetActive(m.Phase==SessionPhase.Finished);
            if(m.Phase==SessionPhase.Finished)
            {
                var ranks=EndingService.Rank(m.Players);var winners=ranks.Where(r=>r.Rank==1).Select(r=>r.Player).ToArray();
                finalInfo.text="最終結果 ― 登録者数による順位\n\n"+string.Join("\n",ranks.Select(r=>r.Rank+"位 "+r.Player.DisplayName+"："+r.Player.Subscribers+"人"))+"\n\n"+(winners.Length>1?"同時優勝！ 共催エンディング":"優勝者のエンディング")+"\n"+string.Join(", ",winners.Select(p=>p.DisplayName))+"\n\n";
                foreach(string id in winners.Select(p=>p.CharacterId).Distinct())
                {var c=game.config.players.First(x=>x.characterId==id);if(c.ending)finalInfo.text+=c.ending.title+"\n"+c.ending.text+"\n\n";}
            }
        }
        private void OnDisable(){if(game&&game.Speed)game.Speed.PointerHeld=false;}
    }
}
