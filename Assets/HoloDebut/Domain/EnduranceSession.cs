using System;
namespace HoloDebut.Domain
{
    public enum EndurancePhase { AwaitRoll, Rolling, Moving, AwaitContinuation, Succeeded, Failed, Cancelled }
    public sealed class EnduranceSession : ISubgameSession
    {
        public string ChallengeId {get;}=Guid.NewGuid().ToString("N");
        public string PlayerId=>player.PlayerId;
        public int OriginalNormalPosition {get;}
        public int StartingGameSkill {get;}
        public int StartingStamina {get;}
        public int LoopSize {get;}
        public int InitialBasicRolls {get;}
        public int BasicRollsRemaining {get;private set;}
        public int ExtraRollsRemaining {get;private set;}
        public int ExtraRollsGranted {get;private set;}
        public int UsedRolls {get;private set;}
        public int Position {get;private set;}
        public int LastPhysicalFace {get;private set;}
        public int LastMovement {get;private set;}
        public int RemainingSteps {get;private set;}
        public int StepIndex {get;private set;}
        public long FundsEarned {get;private set;}
        public int ExperienceEarned {get;private set;}
        public long SubscriberBonus {get;private set;}
        public long NextSuccessBonus=>rules.SuccessBonus(UsedRolls+1);
        public long CurrentOrNextSuccessBonus=>Finished?SubscriberBonus:rules.SuccessBonus(Phase==EndurancePhase.Moving?UsedRolls:UsedRolls+1);
        public EndurancePhase Phase {get;private set;}=EndurancePhase.AwaitRoll;
        public bool Finished=>Phase==EndurancePhase.Succeeded||Phase==EndurancePhase.Failed||Phase==EndurancePhase.Cancelled;
        public bool Succeeded=>Phase==EndurancePhase.Succeeded;
        public bool CanRoll=>Phase==EndurancePhase.AwaitRoll&&(BasicRollsRemaining>0||ExtraRollsRemaining>0);
        public bool CanDecline=>Phase==EndurancePhase.AwaitContinuation&&BasicRollsRemaining==0&&ExtraRollsRemaining==0;
        public bool CanUseItem=>(Phase==EndurancePhase.AwaitRoll||Phase==EndurancePhase.AwaitContinuation)&&pendingItem==null&&inventory.Count(player)>0;
        public RollTicket ActiveRoll {get;private set;}
        public int NextPosition=>(Position+1)%LoopSize;
        private readonly PlayerState player;private readonly InventoryService inventory;private readonly RewardService rewards;private readonly EnduranceRules rules;
        private ItemDefinitionData pendingItem;private long nextRoll;
        public EnduranceSession(PlayerState player,InventoryService inventory,RewardService rewards,EnduranceRules rules=null)
        {
            this.player=player??throw new ArgumentNullException(nameof(player));this.inventory=inventory??throw new ArgumentNullException(nameof(inventory));this.rewards=rewards??throw new ArgumentNullException(nameof(rewards));this.rules=rules??new EnduranceRules();
            OriginalNormalPosition=player.CurrentNodeId;StartingGameSkill=player.GameSkill;StartingStamina=player.Stamina;
            LoopSize=this.rules.LoopSize(StartingGameSkill);InitialBasicRolls=EnduranceRules.BasicRolls(StartingStamina);BasicRollsRemaining=InitialBasicRolls;
        }
        public bool TryUseItem(string playerId,ItemDefinitionData item)
        {
            if(playerId!=PlayerId||item==null||pendingItem!=null||
                (Phase!=EndurancePhase.AwaitRoll&&Phase!=EndurancePhase.AwaitContinuation)||inventory.Count(player,item.Id)<1)return false;
            int extra=checked(ExtraRollsRemaining+1),granted=checked(ExtraRollsGranted+1);
            if(!inventory.Consume(player,item.Id))return false;
            pendingItem=item;ExtraRollsRemaining=extra;ExtraRollsGranted=granted;Phase=EndurancePhase.AwaitRoll;return true;
        }
        public bool TryBeginRoll(out RollTicket ticket)
        {
            ticket=default;if(!CanRoll)return false;
            ticket=new RollTicket(ChallengeId,0,++nextRoll,1);ActiveRoll=ticket;Phase=EndurancePhase.Rolling;return true;
        }
        public bool IsActive(RollTicket ticket)=>Phase==EndurancePhase.Rolling&&ActiveRoll.Equals(ticket);
        public bool TryAcceptResult(RollTicket ticket,int physicalFace)
        {
            if(!IsActive(ticket))return false;
            if(physicalFace<1||physicalFace>6)throw new ArgumentOutOfRangeException(nameof(physicalFace));
            int movement=pendingItem==null?physicalFace:pendingItem.Effect.ResolveMovement(physicalFace);
            if(movement<1)throw new InvalidOperationException("Dice item must resolve to positive movement");
            if(BasicRollsRemaining>0)BasicRollsRemaining--;else ExtraRollsRemaining--;
            UsedRolls=checked(UsedRolls+1);LastPhysicalFace=physicalFace;LastMovement=movement;RemainingSteps=movement;StepIndex=0;
            pendingItem=null;Phase=EndurancePhase.Moving;return true;
        }
        public bool TryCompleteStep(RollTicket ticket,int stepIndex)
        {
            if(Phase!=EndurancePhase.Moving||!ActiveRoll.Equals(ticket)||RemainingSteps<=0||stepIndex!=StepIndex+1)return false;
            Position=NextPosition;RemainingSteps--;StepIndex++;return true;
        }
        public bool TryResolveLanding(RollTicket ticket)
        {
            if(Phase!=EndurancePhase.Moving||!ActiveRoll.Equals(ticket)||RemainingSteps!=0)return false;
            var reward=EnduranceRewardResolver.Resolve(rules,Position,UsedRolls);int skillBefore=player.GameSkill;
            rewards.Grant(ticket.EventId+"/landing",player,reward.Kind,reward.Amount);
            if(Position==0)
            {SubscriberBonus=reward.Amount;BasicRollsRemaining=0;ExtraRollsRemaining=0;Phase=EndurancePhase.Succeeded;}
            else
            {
                if(reward.Kind==RewardKind.Funds)FundsEarned=checked(FundsEarned+reward.Amount);
                if(reward.Kind==RewardKind.GameExperience)ExperienceEarned=checked(ExperienceEarned+player.GameSkill-skillBefore);
                Phase=BasicRollsRemaining>0||ExtraRollsRemaining>0?EndurancePhase.AwaitRoll:inventory.Count(player)>0?EndurancePhase.AwaitContinuation:EndurancePhase.Failed;
            }
            return true;
        }
        public bool TryDeclineContinuation()
        {if(!CanDecline)return false;BasicRollsRemaining=0;ExtraRollsRemaining=0;pendingItem=null;Phase=EndurancePhase.Failed;return true;}
        public void Cancel(){if(Finished)return;BasicRollsRemaining=0;ExtraRollsRemaining=0;pendingItem=null;Phase=EndurancePhase.Cancelled;}
    }
}
