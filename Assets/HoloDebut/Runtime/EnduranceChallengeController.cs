using System;
using System.Collections;
using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    public sealed class EnduranceChallengeController : MonoBehaviour
    {
        public EnduranceSession Session {get;private set;}
        private SessionConfig config;private DiceRollController dice;private PawnMovement movement;
        private GameObject root;private EnduranceBoardGenerator board;private Transform pawn;private int generation;
        public void Initialize(SessionConfig sessionConfig,DiceRollController sharedDice,PawnMovement sharedMovement)
        {config=sessionConfig;dice=sharedDice;movement=sharedMovement;}
        public void Begin(PlayerState player,InventoryService inventory,RewardService rewards)
        {
            Cancel();generation++;Session=new EnduranceSession(player,inventory,rewards,config.endurance.CreateRules());
            root=new GameObject("EnduranceBoardRoot");root.transform.SetParent(transform,false);
            board=root.AddComponent<EnduranceBoardGenerator>();board.Generate(Session.LoopSize,config.endurance,config.tilePrefab);
            var p=Instantiate(config.pawnPrefab,root.transform);p.name="EndurancePawn";pawn=p.transform;pawn.position=board.Anchor(0);
            p.GetComponent<PawnVisual>().Initialize(Color.cyan,player.DisplayName.Split(' ')[0]);
        }
        public IEnumerator RollOnce(Func<bool> hostActive,Action showBoard)
        {
            var session=Session;int run=generation;
            if(session==null||!session.TryBeginRoll(out var ticket))yield break;
            bool Active()=>run==generation&&hostActive()&&session==Session&&!session.Finished;
            int[] faces=null;
            yield return dice.Roll(ticket,()=>Active()&&session.IsActive(ticket),f=>faces=f);
            if(!Active()||faces==null||!session.TryAcceptResult(ticket,faces[0]))yield break;
            yield return new WaitForSeconds(config.resultSeconds);if(!Active())yield break;
            showBoard();
            while(session.RemainingSteps>0)
            {
                yield return movement.MoveStep(pawn,board.Anchor(session.NextPosition),config.stepSeconds,Active);
                if(!Active()||!session.TryCompleteStep(ticket,session.StepIndex+1))yield break;
                yield return new WaitForSeconds(config.stepPauseSeconds);
            }
            if(Active())session.TryResolveLanding(ticket);
        }
        public void Hide(){if(root)root.SetActive(false);}
        public void Cancel(){generation++;Session?.Cancel();if(root){root.SetActive(false);Destroy(root);}root=null;}
        private void OnDestroy()=>Cancel();
    }
}
