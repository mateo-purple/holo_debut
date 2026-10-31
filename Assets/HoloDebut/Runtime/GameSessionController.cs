using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HoloDebut.Domain;
using HoloDebut.Presentation;

namespace HoloDebut.Runtime
{
    public sealed class GameSessionController : MonoBehaviour
    {
        public SessionConfig config;
        public GameSessionModel Model {get;private set;}
        public DiceRollController Dice {get;private set;}
        public GameSpeedController Speed {get;private set;}
        public EnduranceChallengeController Endurance {get;private set;}
        public string Message {get;private set;}="";
        public string FatalError {get;private set;}
        public bool Busy {get;private set;}
        private Transform boardRoot,playersRoot,arenaRoot,cameraRoot;
        private readonly List<Transform> pawns=new List<Transform>();
        private Camera boardCamera,diceCamera,enduranceCamera;
        private PawnMovement movement;
        private PrototypePresenter presenter;
        private int generation;
        private bool wasPhysicsPaused;
        private readonly System.Random eventRandom=new System.Random();
        private static readonly Vector3[] offsets={new Vector3(-0.18f,0.1f,-0.18f),new Vector3(0.18f,0.1f,-0.18f),new Vector3(-0.18f,0.1f,0.18f),new Vector3(0.18f,0.1f,0.18f)};
        private Transform Root(string name){var o=new GameObject(name);o.transform.SetParent(transform,false);return o.transform;}
        private void Awake()
        {
            Speed=gameObject.AddComponent<GameSpeedController>();Speed.Pressed=RequestRoll;
            presenter=gameObject.AddComponent<PrototypePresenter>();
            gameObject.AddComponent<DevelopmentOverlay>();
            try{Initialize();}catch(Exception e){Pause(e);}
            presenter.Initialize(this);
        }
        private void Update()
        { bool paused=Dice&&Dice.ErrorPaused;if(paused&&!wasPhysicsPaused)Speed.Clear();wasPhysicsPaused=paused; }
        private void Initialize()
        {
            if(!config)throw new InvalidOperationException("GameRootのconfigにSessionConfigを指定してください");
            string error=config.Validate();if(error!=null)throw new InvalidOperationException(error);
            if(config.board.columns<1||config.board.spacing<=0||config.board.tileSize<=0)throw new InvalidOperationException("盤面の列数・間隔・マス寸法が不正です");
            var players=new List<PlayerState>();
            for(int i=0;i<config.players.Length;i++)
            {
                var c=config.players[i];var p=new PlayerState("p"+(i+1),c.characterId,JapaneseText.PlayerName(i+1,c.japaneseName),config.initialFunds);
                p.SetSkill(SkillKind.Game,config.characterPerformance?c.game:40);p.SetSkill(SkillKind.Chat,config.characterPerformance?c.chat:40);
                p.SetSkill(SkillKind.Song,config.characterPerformance?c.song:40);p.SetSkill(SkillKind.Asmr,config.characterPerformance?c.asmr:40);p.SetSkill(SkillKind.Stamina,c.stamina);players.Add(p);
                p.SetGameGrowthPermille(config.characterPerformance?c.gameGrowthPermille:config.streamPlan.commonGrowthPermille);
            }
            var inventory=new InventoryService(config.inventoryCapacity);
            foreach(var p in players)for(int i=0;i<config.initialCarrots;i++)inventory.Give("initial/"+i,p);
            Model=new GameSessionModel(players,config.board.CreateGraph(),inventory,new RewardService(),new SessionRules
            {MovementSubscribers=config.movementSubscribers,ArrivalBonuses=(long[])config.goals.arrivalBonuses.Clone(),PostGoal=config.postGoal.CreateRules(),StreamSubscribers=config.streamPlan.baseSubscribers,StreamFunds=config.streamPlan.baseFunds});
            boardRoot=Root("BoardRoot");playersRoot=Root("PlayersRoot");arenaRoot=Root("DiceArenaRoot");arenaRoot.position=new Vector3(30,0,0);cameraRoot=Root("CameraRoot");
            foreach(var node in Model.Board.Nodes)
            {
                var tile=Instantiate(config.tilePrefab,boardRoot);tile.name="Node_"+node.Id;tile.transform.localPosition=config.board.Position(node.Id);
                Color color=node.Kind==NodeKind.Goal?Color.yellow:node.Kind==NodeKind.ItemPickup?new Color(1,0.6f,0.2f):node.Kind==NodeKind.Planning?new Color(0.5f,0.9f,0.7f):new Color(0.6f,0.7f,0.85f);
                tile.GetComponent<TileVisual>().Initialize(node.Id+(node.Kind==NodeKind.ItemPickup?"\n道具":node.Kind==NodeKind.Planning?"\n企画":node.Kind==NodeKind.Start?"\n開始":node.Kind==NodeKind.Goal?"\nゴール":""),color,config.board.tileSize,config.board.tileHeight);
            }
            for(int i=0;i<players.Count;i++)
            {
                var c=config.players[i];var pawn=Instantiate(c.pawnPrefab?c.pawnPrefab:config.pawnPrefab,playersRoot);
                var visual=pawn.GetComponent<PawnVisual>();if(visual)visual.Initialize(c.color,players[i].DisplayName);
                pawn.transform.position=config.board.Position(Model.Board.StartId)+offsets[i];pawns.Add(pawn.transform);
            }
            movement=gameObject.AddComponent<PawnMovement>();
            var bowl=Instantiate(config.bowlPrefab,arenaRoot).GetComponent<BowlSurface>();bowl.Initialize(config.dice);
            Dice=arenaRoot.gameObject.AddComponent<DiceRollController>();Dice.Initialize(config,bowl);
            float size=config.board.spacing*config.board.columns;Vector3 center=new Vector3(size/2,0,size/2);
            boardCamera=CreateCamera("BoardCamera",center+new Vector3(0,size,-size*0.75f),center,0.1f,100);boardCamera.orthographic=true;boardCamera.orthographicSize=size*0.7f;
            diceCamera=CreateCamera("DiceCamera",arenaRoot.position+new Vector3(0,0.65f,-0.70f),arenaRoot.position+new Vector3(0,0.06f,0),0.01f,5);diceCamera.fieldOfView=40;
            enduranceCamera=CreateCamera("EnduranceCamera",new Vector3(0,7,-5),Vector3.zero,0.1f,100);enduranceCamera.orthographic=true;enduranceCamera.orthographicSize=4.5f;
            boardCamera.gameObject.AddComponent<AudioListener>();
            var light=Root("Sun").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(55,-25,0);
            RenderSettings.ambientLight=new Color(0.5f,0.5f,0.55f);
            Endurance=gameObject.AddComponent<EnduranceChallengeController>();Endurance.Initialize(config,Dice,movement);
            View(boardCamera);Message="「サイコロを振る」かSpaceで開始。処理中の長押しで2倍速になります。";
        }
        private Camera CreateCamera(string name,Vector3 position,Vector3 target,float near,float far)
        {var o=new GameObject(name);o.transform.SetParent(cameraRoot,false);o.transform.position=position;o.transform.LookAt(target);var c=o.AddComponent<Camera>();c.nearClipPlane=near;c.farClipPlane=far;c.backgroundColor=new Color(0.08f,0.1f,0.15f);return c;}
        private void View(Camera camera){boardCamera.enabled=camera==boardCamera;diceCamera.enabled=camera==diceCamera;enduranceCamera.enabled=camera==enduranceCamera;}
        public void RequestRoll()
        {
            if(Busy||FatalError!=null||Model==null)return;
            if(Model.Phase==SessionPhase.RunningSubgame)
            {
                if(!Endurance.Session.CanRoll)return;
                StartGuarded(RunEnduranceRoll(generation));return;
            }
            if(Model.Phase!=SessionPhase.AwaitRoll)return;
            StartGuarded(RunNormalTurn(generation));
        }
        public void RequestUseItem()
        {
            if(Busy||FatalError!=null||Model==null)return;
            bool used=Model.Phase==SessionPhase.RunningSubgame?Endurance.Session.TryUseItem(Model.CurrentPlayer.PlayerId,config.rocketCarrot.CreateData()):Model.TryUseItem(Model.CurrentPlayer.PlayerId,config.rocketCarrot.CreateData());
            Message=used?"ロケットニンジン使用：次の移動＋"+config.rocketCarrot.moveBonus+"マス"+(Model.Phase==SessionPhase.RunningSubgame?"、追加投球＋1回":""):"道具は使用できません。自分の投球前に、通常は1手番1個・耐久は1投球につき1個までです。";
        }
        public void RequestNormalStream()
        {
            if(Busy||FatalError!=null||Model?.Phase!=SessionPhase.AwaitPlan)return;
            int mult=config.streamPlan.MultiplierNumerator(eventRandom.Next(100));
            if(Model.TryNormalStream(mult)){Message="通常のゲーム配信が終了しました。次のプレイヤーの手番です。";Model.TryCompleteTurn();}
        }
        public void RequestEndurance()
        {
            if(Busy||FatalError!=null||Model?.Phase!=SessionPhase.AwaitPlan)return;
            Endurance.Begin(Model.CurrentPlayer,Model.Inventory,Model.Rewards);
            if(!Model.TryStartSubgame(Endurance.Session)){Endurance.Cancel();return;}
            boardRoot.gameObject.SetActive(false);playersRoot.gameObject.SetActive(false);View(enduranceCamera);Message="耐久配信：脱出口へぴったり停止すれば成功。同じプレイヤーが続けて投球します。";
        }
        public void RequestFinishEndurance()
        {
            if(Busy||FatalError!=null||Model?.Phase!=SessionPhase.RunningSubgame)return;
            if(Endurance.Session.TryDeclineContinuation())
            {Message="耐久配信を終了しました。途中で得た資金と技能経験値は保持します。";FinishEndurance();}
        }
        public void RequestRetry(){if(Dice && Dice.Retry())Message="同じ投球を再試行します。使用済み道具と累積ボーナスは保持します。";}
        public void RestartSession()
        {
            generation++;StopAllCoroutines();Model?.Cancel();if(Dice)Dice.Cancel();if(Endurance)Endurance.Cancel();Speed.Clear();
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}pawns.Clear();
            if(movement)Destroy(movement);if(Endurance)Destroy(Endurance);
            Busy=false;FatalError=null;try{Initialize();}catch(Exception e){Pause(e);}
            presenter.Initialize(this);
        }
        private IEnumerator RunNormalTurn(int run)
        {
            var model=Model;
            while(run==generation)
            {
                if(!model.TryBeginRoll(out var ticket))yield break;
                View(diceCamera);int[] faces=null;
                yield return Dice.Roll(ticket,()=>run==generation&&model==Model&&model.IsActive(ticket),f=>faces=f);
                if(run!=generation||faces==null)yield break;
                long before=model.CurrentPlayer.Subscribers;
                if(!model.TryAcceptResult(ticket,faces))yield break;
                Message=JapaneseText.RollResult(model.LastResult,model.PinzoroBonus,model.PendingMoveBonus,model.PlannedSteps,model.CurrentPlayer.Arrived,model.CurrentPlayer.Subscribers-before);
                yield return new WaitForSeconds(config.resultSeconds);
                if(run!=generation)yield break;
                if(model.Phase==SessionPhase.PreparingPinzoroReroll)continue;
                if(model.Phase==SessionPhase.Moving)
                {
                    View(boardCamera);
                    while(model.RemainingSteps>0)
                    {
                        int next=model.NextNodeId;var pawn=pawns[model.Turns.CurrentPlayerIndex];
                        yield return movement.MoveStep(pawn,config.board.Position(next)+offsets[model.Turns.CurrentPlayerIndex],config.stepSeconds,()=>run==generation&&model.Phase==SessionPhase.Moving);
                        if(run!=generation)yield break;
                        if(!model.TryCompleteStep(ticket,model.StepIndex+1))yield break;
                        yield return new WaitForSeconds(config.stepPauseSeconds);
                    }
                    if(!model.TryResolveLanding(ticket))yield break;
                }
                if(model.Phase==SessionPhase.EndingTurn)model.TryCompleteTurn();
                View(boardCamera);yield break;
            }
        }
        private IEnumerator RunEnduranceRoll(int run)
        {
            View(diceCamera);
            yield return Endurance.RollOnce(()=>run==generation,()=>View(enduranceCamera));
            if(run!=generation)yield break;
            if(Endurance.Session.Finished)
            {Message=Endurance.Session.Succeeded?"耐久配信成功！ 登録者＋"+Endurance.Session.SubscriberBonus+"人":"耐久配信失敗。途中で得た資金と技能経験値は保持します。";yield return new WaitForSeconds(config.resultSeconds);if(run==generation)FinishEndurance();}
        }
        private void FinishEndurance()
        {
            string id=Endurance.Session.ChallengeId;
            if(!Model.TryFinishSubgame(id))return;
            Endurance.Hide();boardRoot.gameObject.SetActive(true);playersRoot.gameObject.SetActive(true);View(boardCamera);Model.TryCompleteTurn();
        }
        private void StartGuarded(IEnumerator routine){Busy=true;StartCoroutine(Guard(routine,generation));}
        private IEnumerator Guard(IEnumerator root,int run)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);
            try
            {
                while(stack.Count>0 && run==generation)
                {
                    var top=stack.Peek();bool next=false;object current=null;Exception failure=null;
                    try{next=top.MoveNext();if(next)current=top.Current;}catch(Exception e){failure=e;}
                    if(failure!=null){Pause(failure);yield break;}
                    if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                    if(current is IEnumerator nested){stack.Push(nested);continue;}
                    yield return current;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();if(run==generation)Busy=false;}
        }
        private void Pause(Exception error){FatalError=error.GetType().Name+": "+error.Message;Message="エラーのため一時停止しています";if(Dice)Dice.Cancel();if(Speed)Speed.Clear();Debug.LogException(error,this);}
        private void OnDestroy(){generation++;Model?.Cancel();if(Dice)Dice.Cancel();if(Speed)Speed.Pressed=null;}
    }
}
