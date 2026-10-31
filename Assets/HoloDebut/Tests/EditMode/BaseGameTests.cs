using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using HoloDebut.Domain;

namespace HoloDebut.Tests
{
    public class BaseGameTests
    {
        public static GameSessionModel Create(BoardGraph board = null, int players = 2)
        {
            var list = Enumerable.Range(1, players).Select(i => new PlayerState("p" + i, "pekora", "P" + i)).ToArray();
            var inv = new InventoryService(); foreach (var p in list) inv.Give("initial", p);
            return new GameSessionModel(list, board ?? BoardGraph.Prototype(false), inv, new RewardService());
        }
        public static RollTicket Accept(GameSessionModel g, params int[] faces)
        {
            Assert.That(g.TryBeginRoll(out var ticket), Is.True);
            Assert.That(g.TryAcceptResult(ticket, faces), Is.True); return ticket;
        }
        public static void FinishMovement(GameSessionModel g, RollTicket t)
        {
            while (g.RemainingSteps > 0) Assert.That(g.TryCompleteStep(t, g.StepIndex + 1), Is.True);
            Assert.That(g.TryResolveLanding(t), Is.True);
        }
        [Test] public void All36ChoHanOutcomes()
        {
            int cho = 0, han = 0;
            for (int a = 1; a <= 6; a++) for (int b = 1; b <= 6; b++)
            {
                int[] raw = { a, b }; var r = DiceRuleEvaluator.Evaluate(raw, false);
                Assert.That(r.BaseMoveSteps, Is.EqualTo(a + b)); Assert.That(raw, Is.EqualTo(new[] { a, b }));
                if (r.Role == DiceRole.Cho) cho++; else han++;
            }
            Assert.That(cho, Is.EqualTo(18)); Assert.That(han, Is.EqualTo(18));
            Assert.That(DiceRuleEvaluator.Evaluate(new[] { 1, 1 }, false).RequiresReroll, Is.False);
        }
        [Test] public void All216ChinchiroOutcomesAndPermutationInvariance()
        {
            var counts = new Dictionary<DiceRole, int>();
            for (int a = 1; a <= 6; a++) for (int b = 1; b <= 6; b++) for (int c = 1; c <= 6; c++)
            {
                int[] raw = { a, b, c }; var r = DiceRuleEvaluator.Evaluate(raw, true);
                counts[r.Role] = counts.TryGetValue(r.Role, out int n) ? n + 1 : 1;
                Assert.That(raw, Is.EqualTo(new[] { a, b, c }));
                var reverse = DiceRuleEvaluator.Evaluate(new[] { c, a, b }, true);
                Assert.That(reverse.Role, Is.EqualTo(r.Role)); Assert.That(reverse.BaseMoveSteps, Is.EqualTo(r.BaseMoveSteps));
            }
            Assert.That(counts[DiceRole.Pinzoro], Is.EqualTo(1)); Assert.That(counts[DiceRole.Triple], Is.EqualTo(5));
            Assert.That(counts[DiceRole.Shigoro], Is.EqualTo(6)); Assert.That(counts[DiceRole.Hifumi], Is.EqualTo(6));
            Assert.That(counts[DiceRole.Pair], Is.EqualTo(90)); Assert.That(counts[DiceRole.NoRole], Is.EqualTo(108));
        }
        [TestCase(0, 3, 4, 7)] [TestCase(5, 3, 4, 12)]
        public void ChoHanMove(int bonus, int a, int b, int expected)
        { Assert.That(MovementResultResolver.Resolve(DiceRuleEvaluator.Evaluate(new[] { a, b }, false), 0, bonus), Is.EqualTo(expected)); }
        [TestCase(3, 0, 6, 6, 6, 21)] [TestCase(3, 0, 4, 5, 6, 14)]
        [TestCase(3, 0, 1, 2, 3, 4)] [TestCase(6, 0, 6, 6, 6, 24)]
        [TestCase(0, 5, 5, 5, 2, 12)] [TestCase(3, 5, 6, 6, 6, 26)]
        [TestCase(6, 5, 6, 6, 6, 29)] [TestCase(0, 5, 1, 2, 3, 6)]
        public void PinzoroAndCarrotExamples(int chain, int item, int a, int b, int c, int expected)
        { Assert.That(MovementResultResolver.Resolve(DiceRuleEvaluator.Evaluate(new[] { a, b, c }, true), chain, item), Is.EqualTo(expected)); }
        [Test] public void RejectsInvalidInputsAndOverflow()
        {
            Assert.Throws<ArgumentNullException>(() => DiceRuleEvaluator.Evaluate(null, true));
            Assert.Throws<ArgumentException>(() => DiceRuleEvaluator.Evaluate(new[] { 1, 2 }, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => DiceRuleEvaluator.Evaluate(new[] { 0, 1 }, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => DiceRuleEvaluator.Evaluate(new[] { 1, 7 }, false));
            Assert.Throws<ArgumentException>(() => MovementResultResolver.Resolve(DiceRuleEvaluator.Evaluate(new[] { 1, 1, 1 }, true), 3, 0));
            Assert.Throws<OverflowException>(() => MovementResultResolver.Resolve(DiceRuleEvaluator.Evaluate(new[] { 2, 2 }, false), long.MaxValue, 0));
        }
        [TestCase(4, 0, 1, 1)] [TestCase(20, 0, 5, 1)] [TestCase(24, 0, 6, 1)] [TestCase(28, 0, 0, 2)]
        public void FourPlayerCalendar(int turns, int playerIndex, int day, int week)
        {
            var g = Create(players: 4);
            for (int i = 0; i < turns; i++)
            {
                var t = Accept(g, g.Turns.Weekend ? new[] { 1, 2, 3 } : new[] { 1, 1 });
                FinishMovement(g, t); Assert.That(g.TryCompleteTurn(), Is.True);
            }
            Assert.That(g.Turns.CurrentPlayerIndex, Is.EqualTo(playerIndex)); Assert.That(g.Turns.DayIndex, Is.EqualTo(day)); Assert.That(g.Turns.Week, Is.EqualTo(week));
        }
        [TestCase(false)] [TestCase(true)] public void GoalTruncatesMovementAndRewardsActualSteps(bool carrot)
        {
            var board = new BoardGraph(new[] {
                new BoardNode(118, NodeKind.Start, new[]{119}), new BoardNode(119, NodeKind.Empty, new[]{120}), new BoardNode(120, NodeKind.Goal, Array.Empty<int>()) }, 118);
            var g = Create(board); var p = g.CurrentPlayer;
            if (carrot) Assert.That(g.TryUseItem(p.PlayerId, ItemDefinitionData.RocketCarrot()), Is.True);
            var t = Accept(g, 3, 4); FinishMovement(g, t);
            Assert.That(p.CurrentNodeId, Is.EqualTo(120)); Assert.That(p.Subscribers, Is.EqualTo(102000)); Assert.That(p.ArrivalRank, Is.EqualTo(1));
            Assert.That(g.TryResolveLanding(t), Is.False); Assert.That(g.TryCompleteStep(t, 3), Is.False);
        }
        [Test] public void ItemsAreAtomicAndRestrictedToOwnPreRollTurn()
        {
            var g = Create(); var p = g.CurrentPlayer; var carrot = ItemDefinitionData.RocketCarrot();
            g.Inventory.Give("extra", p);
            Assert.That(g.TryUseItem("p2", carrot), Is.False);
            Assert.That(g.TryUseItem(p.PlayerId, carrot), Is.True);
            Assert.That(g.TryUseItem(p.PlayerId, carrot), Is.False); Assert.That(g.Inventory.Count(p), Is.EqualTo(1));
            Assert.That(g.TryBeginRoll(out var t), Is.True); Assert.That(g.TryBeginRoll(out _), Is.False);
            Assert.That(g.TryUseItem(p.PlayerId, carrot), Is.False); Assert.That(g.PendingMoveBonus, Is.EqualTo(5));
            Assert.That(g.IsActive(t), Is.True); // invalid physical attempts don't mutate this ticket or the item.
            Assert.That(g.TryAcceptResult(t, new[] { 3, 4 }), Is.True); Assert.That(g.TryAcceptResult(t, new[] { 3, 4 }), Is.False);
            FinishMovement(g, t); Assert.That(p.Subscribers, Is.EqualTo(12000));
            Assert.That(g.TryCompleteTurn(), Is.True); Assert.That(g.PendingMoveBonus, Is.Zero);
            Assert.That(g.TryAcceptResult(t, new[] { 3, 4 }), Is.False);
        }
        [Test] public void NoItemDoesNotConsumeAndPickupIsDeduplicatedAndCapped()
        {
            var p = new PlayerState("p", "pekora", "P"); var inv = new InventoryService();
            Assert.That(inv.Count(p), Is.Zero);
            Assert.That(inv.Give("a", p), Is.True); Assert.That(inv.Give("a", p), Is.False);
            Assert.That(inv.Give("b", p), Is.True); Assert.That(inv.Give("c", p), Is.True); Assert.That(inv.Give("d", p), Is.False);
            Assert.That(inv.Count(p), Is.EqualTo(3));
        }
        [Test] public void PickupOnlyOnLandingNotPassing()
        {
            var board = new BoardGraph(new[] { new BoardNode(0, NodeKind.Start, new[]{1}), new BoardNode(1, NodeKind.ItemPickup, new[]{2}),
                new BoardNode(2, NodeKind.Empty, new[]{3}), new BoardNode(3, NodeKind.Goal, Array.Empty<int>()) });
            var g = Create(board); var p = g.CurrentPlayer; var t = Accept(g, 1, 1); FinishMovement(g, t);
            Assert.That(g.Inventory.Count(p), Is.EqualTo(1));
        }
        [Test] public void PostGoalTurnRetainsCalendarAndFinalArrivalStopsImmediately()
        {
            var g = Create(BoardGraph.Prototype(false, 6));
            var p1 = g.CurrentPlayer; FinishMovement(g, Accept(g, 3, 3)); g.TryCompleteTurn();
            FinishMovement(g, Accept(g, 1, 1)); g.TryCompleteTurn();
            Assert.That(g.CurrentPlayer, Is.SameAs(p1)); Assert.That(g.TryUseItem("p1", ItemDefinitionData.RocketCarrot()), Is.False);
            Accept(g, 1, 1, 1); Assert.That(g.PinzoroBonus, Is.EqualTo(30000));
            Assert.That(g.TryBeginRoll(out var t), Is.True); Assert.That(g.IsActive(t), Is.True);
            g.TryAcceptResult(t, new[] { 6, 6, 6 }); Assert.That(p1.Subscribers, Is.EqualTo(154000)); g.TryCompleteTurn();
            FinishMovement(g, Accept(g, 2, 2)); g.TryCompleteTurn();
            Assert.That(g.Phase, Is.EqualTo(SessionPhase.Finished)); Assert.That(g.Turns.CompletedTurns, Is.EqualTo(3));
            Assert.That(g.TryBeginRoll(out _), Is.False); Assert.That(g.TryCompleteTurn(), Is.False);
        }
        [Test] public void SubscriberTiesIgnoreFundsArrivalAndCharacter()
        {
            var a = new PlayerState("a", "pekora", "A", 100); var b = new PlayerState("b", "pekora", "B", 900);
            var rank = EndingService.Rank(new[] { a, b }); Assert.That(rank.All(p => p.Rank == 1), Is.True);
        }
        [Test] public void RewardsAreDeduplicatedCappedAndChecked()
        {
            var p = new PlayerState("p", "x", "P"); var r = new RewardService();
            Assert.That(r.Grant("e", p, RewardKind.Funds, 100), Is.True); Assert.That(r.Grant("e", p, RewardKind.Funds, 100), Is.False);
            p.SetSkill(SkillKind.Game, 98); r.Grant("e", p, RewardKind.GameExperience, 5); Assert.That(p.GameSkill, Is.EqualTo(100));
            r.Grant("big", p, RewardKind.Subscribers, long.MaxValue);
            Assert.Throws<OverflowException>(() => r.Grant("overflow", p, RewardKind.Subscribers, 1));
            Assert.That(r.History.Count, Is.EqualTo(3));
        }
        [Test] public void NormalBoardValidation()
        {
            Assert.That(BoardGraph.Prototype().Nodes.Count, Is.EqualTo(121));
            Assert.Throws<ArgumentException>(() => new BoardGraph(new[]{new BoardNode(0, NodeKind.Start, new[]{1})}));
            Assert.Throws<ArgumentException>(() => new BoardGraph(new[]{new BoardNode(0, NodeKind.Start, new[]{0})}));
            Assert.Throws<ArgumentException>(() => new BoardGraph(new[]{new BoardNode(0, NodeKind.Empty, Array.Empty<int>())}));
            Assert.Throws<ArgumentException>(() => new BoardGraph(new[]{new BoardNode(0, NodeKind.Goal, Array.Empty<int>()),new BoardNode(0, NodeKind.Goal, Array.Empty<int>())}));
        }
        [Test] public void HoldUsesRealtimeAndCannotCrossTurnBoundary()
        {
            var input = new ProgressInput(); Assert.That(input.Press(10), Is.True);
            Assert.That(input.FastForward(10.19), Is.False); Assert.That(input.FastForward(10.21), Is.True);
            Assert.That(input.Press(12), Is.False); input.Release(); Assert.That(input.FastForward(12), Is.False); Assert.That(input.Press(12), Is.True);
        }
        [Test] public void CancelRejectsLateCallbacks()
        { var g = Create(); g.TryBeginRoll(out var t); g.Cancel(); Assert.That(g.TryAcceptResult(t, new[]{1,1}), Is.False); }
        [Test] public void PinzoroChainKeepsItemAndBonusAcrossInvalidAttempts()
        {
            var g=Create();
            for(int i=0;i<10;i++){FinishMovement(g,Accept(g,1,1));g.TryCompleteTurn();}
            Assert.That(g.Turns.Weekend,Is.True);var p=g.CurrentPlayer;
            Assert.That(g.TryUseItem(p.PlayerId,ItemDefinitionData.RocketCarrot()),Is.True);
            Accept(g,1,1,1);Assert.That(g.PinzoroBonus,Is.EqualTo(3));
            g.TryBeginRoll(out var t);Assert.That(g.IsActive(t),Is.True);Assert.That(g.PinzoroBonus,Is.EqualTo(3));Assert.That(g.PendingMoveBonus,Is.EqualTo(5));
            Assert.That(g.TryUseItem(p.PlayerId,ItemDefinitionData.RocketCarrot()),Is.False);
            g.TryAcceptResult(t,new[]{1,1,1});var last=Accept(g,6,6,6);Assert.That(g.PlannedSteps,Is.EqualTo(29));
            FinishMovement(g,last);Assert.That(g.Inventory.Count(p),Is.Zero);Assert.That(g.TryResolveLanding(last),Is.False);
        }
        [TestCase(2)][TestCase(3)][TestCase(4)] public void FullNormalGameReachesAllPlayersAndEndsWithoutExtraTurn(int players)
        {
            var g=Create(players:players);int n=0;
            while(g.Phase!=SessionPhase.Finished&&n++<200)
            {
                bool three=g.CurrentPlayer.Arrived||g.Turns.Weekend;
                var t=Accept(g,three?new[]{6,6,6}:new[]{6,6});
                if(g.Phase==SessionPhase.Moving)FinishMovement(g,t);
                Assert.That(g.TryCompleteTurn(),Is.True);
            }
            Assert.That(g.Phase,Is.EqualTo(SessionPhase.Finished));Assert.That(g.Players.All(p=>p.Arrived),Is.True);
            Assert.That(g.Players.Select(p=>p.ArrivalRank).OrderBy(x=>x),Is.EqualTo(Enumerable.Range(1,players)));
            Assert.That(g.TryBeginRoll(out _),Is.False);
            var movement=g.Rewards.History.Where(r=>r.SourceEventId.Contains("/step/"));Assert.That(movement.Sum(r=>r.AppliedAmount),Is.EqualTo(120000L*players));
        }
        [Test] public void ItemPickupLandingIsOnceAndNormalUnownedItemIsRejected()
        {
            var board=new BoardGraph(new[]{new BoardNode(0,NodeKind.Start,new[]{1}),new BoardNode(1,NodeKind.Empty,new[]{2}),new BoardNode(2,NodeKind.ItemPickup,new[]{3}),new BoardNode(3,NodeKind.Goal,Array.Empty<int>())});
            var g=Create(board);var p=g.CurrentPlayer;var t=Accept(g,1,1);FinishMovement(g,t);
            Assert.That(g.Inventory.Count(p),Is.EqualTo(2));Assert.That(g.TryResolveLanding(t),Is.False);Assert.That(g.Inventory.Count(p),Is.EqualTo(2));
            var empty=new GameSessionModel(new[]{new PlayerState("a","x","A"),new PlayerState("b","x","B")},BoardGraph.Prototype(),new InventoryService(),new RewardService());
            Assert.That(empty.TryUseItem("a",ItemDefinitionData.RocketCarrot()),Is.False);Assert.That(empty.Inventory.Count(empty.CurrentPlayer),Is.Zero);
        }
    }
}
