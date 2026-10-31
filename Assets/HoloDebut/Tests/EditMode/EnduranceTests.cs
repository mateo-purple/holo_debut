using System;
using NUnit.Framework;
using HoloDebut.Domain;
namespace HoloDebut.Tests
{
    public class EnduranceTests
    {
        private PlayerState player;private InventoryService inventory;private RewardService rewards;
        private EnduranceSession Create(int game=50,int stamina=50,int items=0,EnduranceRules rules=null)
        {
            player=new PlayerState("p","pekora","P");player.SetSkill(SkillKind.Game,game);player.SetSkill(SkillKind.Stamina,stamina);
            inventory=new InventoryService();rewards=new RewardService();for(int i=0;i<items;i++)inventory.Give("i"+i,player);
            return new EnduranceSession(player,inventory,rewards,rules);
        }
        private static RollTicket Roll(EnduranceSession e,int face)
        {
            Assert.That(e.TryBeginRoll(out var t),Is.True);Assert.That(e.TryAcceptResult(t,face),Is.True);
            while(e.RemainingSteps>0)Assert.That(e.TryCompleteStep(t,e.StepIndex+1),Is.True);
            Assert.That(e.TryResolveLanding(t),Is.True);return t;
        }
        [TestCase(0,1)][TestCase(19,1)][TestCase(20,2)][TestCase(39,2)][TestCase(40,3)]
        [TestCase(59,3)][TestCase(60,4)][TestCase(79,4)][TestCase(80,5)][TestCase(100,5)]
        public void StaminaBoundaries(int stamina,int expected)=>Assert.That(EnduranceRules.BasicRolls(stamina),Is.EqualTo(expected));
        [TestCase(0,12)][TestCase(9,12)][TestCase(10,11)][TestCase(19,11)][TestCase(20,10)][TestCase(29,10)]
        [TestCase(30,8)][TestCase(39,8)][TestCase(40,6)][TestCase(59,6)][TestCase(60,5)][TestCase(69,5)]
        [TestCase(70,4)][TestCase(79,4)][TestCase(80,3)][TestCase(94,3)][TestCase(95,2)][TestCase(100,2)]
        public void SkillBoundaries(int skill,int expected)=>Assert.That(new EnduranceRules().LoopSize(skill),Is.EqualTo(expected));
        [Test] public void StartingAtExitIsNotSuccessAndSixEscapesSixNodeLoop()
        {var e=Create();Assert.That(e.Finished,Is.False);Assert.That(player.Subscribers,Is.Zero);Roll(e,6);Assert.That(e.Succeeded,Is.True);Assert.That(player.Subscribers,Is.EqualTo(50000));}
        [Test] public void EscapeRewardPreviewUsesCurrentRollWhileMovingAndActualBonusAfterSuccess()
        {
            var e=Create();Assert.That(e.CurrentOrNextSuccessBonus,Is.EqualTo(50000));
            Roll(e,3);Assert.That(e.CurrentOrNextSuccessBonus,Is.EqualTo(35000));
            e.TryBeginRoll(out var t);e.TryAcceptResult(t,3);
            Assert.That(e.CurrentOrNextSuccessBonus,Is.EqualTo(35000));
            Assert.That(e.NextSuccessBonus,Is.EqualTo(25000));
            while(e.RemainingSteps>0)e.TryCompleteStep(t,e.StepIndex+1);
            e.TryResolveLanding(t);Assert.That(e.CurrentOrNextSuccessBonus,Is.EqualTo(player.Subscribers));
        }
        [Test] public void FailedChallengeRewardPreviewIsZero()
        {var e=Create(stamina:0);Roll(e,1);Assert.That(e.CurrentOrNextSuccessBonus,Is.Zero);}
        [Test] public void ThreePlusThreeEscapesWithSecondThrowBonusAndOnlyLandingXP()
        {
            var e=Create();Roll(e,3);Assert.That(e.Position,Is.EqualTo(3));Assert.That(player.GameSkill,Is.EqualTo(55));Assert.That(player.Funds,Is.EqualTo(50000));
            Roll(e,3);Assert.That(e.Succeeded,Is.True);Assert.That(player.Subscribers,Is.EqualTo(35000));Assert.That(e.ExperienceEarned,Is.EqualTo(5));
        }
        [TestCase(2)][TestCase(4)][TestCase(6)] public void EvenRollEscapesTwoNodeLoop(int face)
        {var e=Create(95);Roll(e,face);Assert.That(e.Succeeded,Is.True);}
        [Test] public void TwelveNodeLoopSixStopsAtSixAndFundsOnlyForLanding()
        {var e=Create(0);Roll(e,6);Assert.That(e.Finished,Is.False);Assert.That(e.Position,Is.EqualTo(6));Assert.That(player.Funds,Is.EqualTo(60000));Assert.That(player.GameSkill,Is.Zero);Assert.That(player.Subscribers,Is.Zero);}
        [Test] public void PassingExitIsNotSuccess()
        {var e=Create();Roll(e,5);Roll(e,2);Assert.That(e.Position,Is.EqualTo(1));Assert.That(e.Succeeded,Is.False);Assert.That(e.UsedRolls,Is.EqualTo(2));Assert.That(player.Subscribers,Is.Zero);}
        [Test] public void RocketCarrotPreservesFaceMovesEightAddsOneRollConsumesOneOnlyOnce()
        {
            var e=Create(items:2);Assert.That(e.TryUseItem("p",ItemDefinitionData.RocketCarrot()),Is.True);
            Assert.That(e.ExtraRollsRemaining,Is.EqualTo(1));Assert.That(inventory.Count(player),Is.EqualTo(1));
            Assert.That(e.TryUseItem("p",ItemDefinitionData.RocketCarrot()),Is.False);
            Roll(e,3);Assert.That(e.LastPhysicalFace,Is.EqualTo(3));Assert.That(e.LastMovement,Is.EqualTo(8));Assert.That(e.Position,Is.EqualTo(2));
            Roll(e,1);Assert.That(e.LastMovement,Is.EqualTo(1));Assert.That(e.Position,Is.EqualTo(3));
            Assert.That(e.TryUseItem("p",ItemDefinitionData.RocketCarrot()),Is.True);Assert.That(inventory.Count(player),Is.Zero);
        }
        [Test] public void InvalidAttemptsDoNotSpendRollOrReapplyItemAndLateResultsRejected()
        {
            var e=Create(items:1);e.TryUseItem("p",ItemDefinitionData.RocketCarrot());e.TryBeginRoll(out var t);
            for(int i=0;i<10;i++)Assert.That(e.IsActive(t),Is.True);
            Assert.That(e.UsedRolls,Is.Zero);Assert.That(e.BasicRollsRemaining,Is.EqualTo(3));Assert.That(e.ExtraRollsRemaining,Is.EqualTo(1));Assert.That(inventory.Count(player),Is.Zero);
            Assert.That(e.TryBeginRoll(out _),Is.False);Assert.That(e.TryUseItem("p",ItemDefinitionData.RocketCarrot()),Is.False);
            Assert.That(e.TryAcceptResult(t,3),Is.True);Assert.That(e.TryAcceptResult(t,3),Is.False);
            Assert.That(e.LastMovement,Is.EqualTo(8));while(e.RemainingSteps>0)e.TryCompleteStep(t,e.StepIndex+1);
            Assert.That(e.TryResolveLanding(t),Is.True);Assert.That(e.TryResolveLanding(t),Is.False);Assert.That(player.Funds,Is.EqualTo(60000));
            e.TryBeginRoll(out var next);Assert.That(e.TryAcceptResult(t,6),Is.False);Assert.That(e.TryAcceptResult(next,1),Is.True);
        }
        [Test] public void OwningItemsDoesNotAddRollsAndDecliningRetainsFunds()
        {
            var e=Create(stamina:0,items:1);Assert.That(e.ExtraRollsRemaining,Is.Zero);Roll(e,2);
            Assert.That(e.Phase,Is.EqualTo(EndurancePhase.AwaitContinuation));Assert.That(e.CanRoll,Is.False);Assert.That(e.TryDeclineContinuation(),Is.True);
            Assert.That(e.Finished,Is.True);Assert.That(e.Succeeded,Is.False);Assert.That(player.Funds,Is.EqualTo(60000));Assert.That(player.Subscribers,Is.Zero);Assert.That(inventory.Count(player),Is.EqualTo(1));
        }
        [Test] public void ExtraAttemptRequiresRealEffectAndCannotFailWhileRollsRemain()
        {
            var e=Create(stamina:0,items:1);Assert.That(e.TryDeclineContinuation(),Is.False);Roll(e,1);
            Assert.That(e.TryUseItem("other",ItemDefinitionData.RocketCarrot()),Is.False);
            Assert.That(e.TryUseItem("p",ItemDefinitionData.RocketCarrot()),Is.True);Assert.That(e.CanDecline,Is.False);Assert.That(inventory.Count(player),Is.Zero);
            Roll(e,1);Assert.That(e.LastMovement,Is.EqualTo(6));Assert.That(e.UsedRolls,Is.EqualTo(2));Assert.That(e.Phase,Is.EqualTo(EndurancePhase.Failed));Assert.That(e.ExperienceEarned,Is.EqualTo(10));
        }
        [Test] public void NoItemsAutoFailsAtExhaustionAndRetainsXP()
        {var e=Create(stamina:0);Roll(e,1);Assert.That(e.Phase,Is.EqualTo(EndurancePhase.Failed));Assert.That(player.GameSkill,Is.EqualTo(55));Assert.That(player.Subscribers,Is.Zero);}
        [Test] public void SuccessDiscardsAllRemainingRollsAndCannotContinue()
        {var e=Create(items:1);e.TryUseItem("p",ItemDefinitionData.RocketCarrot());var t=Roll(e,1);Assert.That(e.Succeeded,Is.True);Assert.That(e.BasicRollsRemaining,Is.Zero);Assert.That(e.ExtraRollsRemaining,Is.Zero);Assert.That(e.TryBeginRoll(out _),Is.False);Assert.That(e.TryResolveLanding(t),Is.False);Assert.That(player.Subscribers,Is.EqualTo(50000));}
        [TestCase(1,50000)][TestCase(2,35000)][TestCase(3,25000)][TestCase(4,15000)][TestCase(5,10000)][TestCase(6,10000)]
        public void ActualThrowNumberSetsSuccessReward(int successfulThrow,long expected)
        {
            var e=Create(stamina:100,items:1);
            for(int i=1;i<successfulThrow;i++)
            {
                if(i==5)e.TryUseItem("p",ItemDefinitionData.RocketCarrot());
                Roll(e,1);
            }
            int final=6-e.Position;Roll(e,final);Assert.That(e.Succeeded,Is.True);Assert.That(player.Subscribers,Is.EqualTo(expected));Assert.That(e.UsedRolls,Is.EqualTo(successfulThrow));
        }
        [Test] public void GrowthAndConfigMutationCannotChangeChallengeConditions()
        {
            int[] maxima={9,19,29,39,59,69,79,94,100},sizes={12,11,10,8,6,5,4,3,2};
            var rules=new EnduranceRules(maxima,sizes);var e=Create(59,59,rules:rules);maxima[4]=40;sizes[4]=12;
            Roll(e,1);Assert.That(player.GameSkill,Is.EqualTo(64));player.SetSkill(SkillKind.Stamina,100);
            Assert.That(e.LoopSize,Is.EqualTo(6));Assert.That(e.InitialBasicRolls,Is.EqualTo(3));Assert.That(e.BasicRollsRemaining,Is.EqualTo(2));
            Assert.That(new EnduranceSession(player,inventory,rewards,rules).LoopSize,Is.EqualTo(5));
        }
        [Test] public void SameNodeOnDifferentThrowsGivesRewardAgainAndCapsSkill()
        {var e=Create(99);Roll(e,1);Assert.That(player.GameSkill,Is.EqualTo(100));Roll(e,2);Assert.That(e.Position,Is.EqualTo(1));Assert.That(rewards.History.Count,Is.EqualTo(2));Assert.That(e.ExperienceEarned,Is.EqualTo(1));}
        [Test] public void CancelAndOldChallengeTicketsCannotMutateNewSession()
        {var old=Create();old.TryBeginRoll(out var t);old.Cancel();Assert.That(old.TryAcceptResult(t,6),Is.False);var next=new EnduranceSession(player,inventory,rewards);next.TryBeginRoll(out _);Assert.That(next.TryAcceptResult(t,6),Is.False);}
        [TestCase(false)][TestCase(true)] public void NormalPositionCalendarAndTurnArePreservedUntilSubgameEnds(bool success)
        {
            var g=BaseGameTests.Create();var t=BaseGameTests.Accept(g,5,5);BaseGameTests.FinishMovement(g,t);
            // This board explicitly adds the plan node at 10.
            Assert.That(g.Phase,Is.EqualTo(SessionPhase.EndingTurn));
            g=BaseGameTests.Create(BoardGraph.Prototype(true));t=BaseGameTests.Accept(g,5,5);BaseGameTests.FinishMovement(g,t);
            var p=g.CurrentPlayer;p.SetSkill(SkillKind.Stamina,0);var e=new EnduranceSession(p,g.Inventory,g.Rewards);
            Assert.That(g.TryStartSubgame(e),Is.True);Assert.That(g.TryBeginRoll(out _),Is.False);Assert.That(g.TryCompleteTurn(),Is.False);
            Assert.That(g.Turns.CompletedTurns,Is.Zero);Assert.That(g.CurrentPlayer,Is.SameAs(p));
            Roll(e,success?6:1);if(!e.Finished)e.TryDeclineContinuation();
            Assert.That(p.CurrentNodeId,Is.EqualTo(10));Assert.That(p.ArrivalRank,Is.Zero);Assert.That(g.Turns.DayIndex,Is.Zero);
            Assert.That(g.TryFinishSubgame(e.ChallengeId),Is.True);Assert.That(g.TryFinishSubgame(e.ChallengeId),Is.False);Assert.That(g.TryCompleteTurn(),Is.True);
            Assert.That(g.Turns.CompletedTurns,Is.EqualTo(1));Assert.That(g.CurrentPlayer.PlayerId,Is.EqualTo("p2"));
            Assert.That(p.Subscribers,Is.EqualTo(success?60000:10000));
        }
        [Test] public void PlanMustBeSelectedCannotStartFromOrdinaryTile()
        {var g=BaseGameTests.Create();Assert.That(g.TryStartSubgame(new EnduranceSession(g.CurrentPlayer,g.Inventory,g.Rewards)),Is.False);}
        [Test] public void NormalStreamChoiceIsFunctionalAndEndsPlanTurn()
        {var g=BaseGameTests.Create(BoardGraph.Prototype(true));BaseGameTests.FinishMovement(g,BaseGameTests.Accept(g,5,5));Assert.That(g.TryNormalStream(2),Is.True);Assert.That(g.TryNormalStream(2),Is.False);Assert.That(g.CurrentPlayer.Funds,Is.EqualTo(55000));Assert.That(g.TryCompleteTurn(),Is.True);}
        [Test] public void BadConfigurationRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>EnduranceRules.BasicRolls(-1));Assert.Throws<ArgumentOutOfRangeException>(()=>new EnduranceRules().LoopSize(101));
            Assert.Throws<ArgumentException>(()=>new EnduranceRules(new[]{100},new[]{2}));
            Assert.Throws<ArgumentException>(()=>new EnduranceRules(new[]{100},new[]{13}));
            Assert.Throws<ArgumentException>(()=>new EnduranceRules(success:new long[]{-1}));
        }
        private sealed class FixedFaceEffect : IDiceItemEffect
        {public int ResolveMovement(int physicalFace)=>4;}
        [Test] public void FutureEffectContractExecutesEffectAndExtraRollTogether()
        {
            var e=Create();inventory.Give("future",player,"test_only_fixed");
            // Only the effect contract is exercised; this item is never registered in gameplay.
            var effect=new ItemDefinitionData("test_only_fixed",new FixedFaceEffect());
            Assert.That(e.TryUseItem("p",effect),Is.True);Roll(e,1);Assert.That(e.LastPhysicalFace,Is.EqualTo(1));Assert.That(e.LastMovement,Is.EqualTo(4));Assert.That(e.ExtraRollsGranted,Is.EqualTo(1));
        }
    }
}
