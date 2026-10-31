using System;
using System.Collections.Generic;
using System.Linq;

namespace HoloDebut.Domain
{
    public enum SessionPhase { AwaitRoll, Rolling, PreparingPinzoroReroll, Moving, EndingTurn, AwaitPlan, RunningSubgame, Finished, Cancelled }
    public readonly struct RollTicket : IEquatable<RollTicket>
    {
        public readonly string SessionId;
        public readonly long TurnId, RollId;
        public readonly int DiceCount;
        public RollTicket(string sessionId, long turnId, long rollId, int count)
        { SessionId = sessionId; TurnId = turnId; RollId = rollId; DiceCount = count; }
        public bool Equals(RollTicket other) => SessionId == other.SessionId && TurnId == other.TurnId && RollId == other.RollId && DiceCount == other.DiceCount;
        public override bool Equals(object obj) => obj is RollTicket t && Equals(t);
        public override int GetHashCode() => (SessionId, TurnId, RollId, DiceCount).GetHashCode();
        public string EventId => SessionId + "/" + TurnId + "/" + RollId;
    }
    public sealed class SessionRules
    {
        public long MovementSubscribers = 1000;
        public long[] ArrivalBonuses = { 100000, 60000, 30000, 10000 };
        public PostGoalRewardRules PostGoal = new PostGoalRewardRules();
        public long StreamSubscribers = 5000, StreamFunds = 5000;
    }
    public sealed class GameSessionModel
    {
        public string SessionId { get; } = Guid.NewGuid().ToString("N");
        public IReadOnlyList<PlayerState> Players { get; }
        public PlayerState CurrentPlayer => Players[Turns.CurrentPlayerIndex];
        public TurnService Turns { get; }
        public BoardGraph Board { get; }
        public InventoryService Inventory { get; }
        public RewardService Rewards { get; }
        public SessionPhase Phase { get; private set; } = SessionPhase.AwaitRoll;
        public DiceResult LastResult { get; private set; }
        public long PinzoroBonus { get; private set; }
        public long PendingMoveBonus { get; private set; }
        public long PlannedSteps { get; private set; }
        public long RemainingSteps { get; private set; }
        public int StepIndex { get; private set; }
        public RollTicket ActiveRoll { get; private set; }
        public ISubgameSession ActiveSubgame { get; private set; }
        public bool ItemUsed { get; private set; }
        private readonly SessionRules rules;
        private readonly GoalService goals;
        private long nextRoll;

        public GameSessionModel(IList<PlayerState> players, BoardGraph board, InventoryService inventory, RewardService rewards, SessionRules rules = null)
        {
            if (players == null || players.Count < 2 || players.Count > 4 || players.Any(p => p == null) || players.Select(p => p.PlayerId).Distinct().Count() != players.Count)
                throw new ArgumentException("Need 2-4 unique player IDs");
            Players = new List<PlayerState>(players).AsReadOnly(); Board = board ?? throw new ArgumentNullException(nameof(board));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory)); Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            this.rules = rules ?? new SessionRules(); Turns = new TurnService(players.Count); goals = new GoalService(Rewards, this.rules.ArrivalBonuses);
            foreach (var p in Players) p.CurrentNodeId = Board.StartId;
        }
        public bool CanUseItem(string playerId) => Phase == SessionPhase.AwaitRoll && CurrentPlayer.PlayerId == playerId && !CurrentPlayer.Arrived && !ItemUsed && Inventory.Count(CurrentPlayer) > 0;
        public bool TryUseItem(string playerId, ItemDefinitionData item)
        {
            if (!CanUseItem(playerId) || item == null || item.Id != ItemDefinitionData.RocketCarrotId) return false;
            int bonus = item.Effect.ResolveMovement(0);
            if (bonus < 0 || !Inventory.Consume(CurrentPlayer, item.Id)) return false;
            PendingMoveBonus = bonus; ItemUsed = true; return true;
        }
        public bool TryBeginRoll(out RollTicket ticket)
        {
            ticket = default;
            if (Phase != SessionPhase.AwaitRoll && Phase != SessionPhase.PreparingPinzoroReroll) return false;
            ticket = new RollTicket(SessionId, Turns.CompletedTurns, ++nextRoll, CurrentPlayer.Arrived || Turns.Weekend ? 3 : 2);
            ActiveRoll = ticket; Phase = SessionPhase.Rolling; return true;
        }
        // Invalid attempts remain attached to the same ticket. No economy or rule mutation occurs.
        public bool IsActive(RollTicket ticket) => Phase == SessionPhase.Rolling && ActiveRoll.Equals(ticket);
        public bool TryAcceptResult(RollTicket ticket, int[] faces)
        {
            if (!IsActive(ticket)) return false;
            var result = DiceRuleEvaluator.Evaluate(faces, ticket.DiceCount == 3);
            LastResult = result;
            if (result.RequiresReroll)
            {
                PinzoroBonus = checked(PinzoroBonus + (CurrentPlayer.Arrived ? rules.PostGoal.Pinzoro : result.BonusStepsToAdd));
                Phase = SessionPhase.PreparingPinzoroReroll;
            }
            else if (CurrentPlayer.Arrived)
            {
                Rewards.Grant(ticket.EventId + "/post-goal", CurrentPlayer, RewardKind.Subscribers, checked(PinzoroBonus + rules.PostGoal.Evaluate(result)));
                Phase = SessionPhase.EndingTurn;
            }
            else
            {
                PlannedSteps = MovementResultResolver.Resolve(result, PinzoroBonus, PendingMoveBonus);
                RemainingSteps = PlannedSteps; StepIndex = 0; Phase = SessionPhase.Moving;
            }
            return true;
        }
        public int NextNodeId
        {
            get
            {
                if (Phase != SessionPhase.Moving || RemainingSteps <= 0) throw new InvalidOperationException("No pending movement");
                var next = Board[CurrentPlayer.CurrentNodeId].NextIds;
                if (next.Count != 1) throw new InvalidOperationException("Prototype requires a single selected route");
                return next[0];
            }
        }
        public bool TryCompleteStep(RollTicket ticket, int stepIndex)
        {
            if (Phase != SessionPhase.Moving || !ActiveRoll.Equals(ticket) || stepIndex != StepIndex + 1 || RemainingSteps <= 0) return false;
            int next = NextNodeId;
            Rewards.Grant(ticket.EventId + "/step/" + stepIndex, CurrentPlayer, RewardKind.Subscribers, rules.MovementSubscribers);
            CurrentPlayer.CurrentNodeId = next; StepIndex++; RemainingSteps--;
            if (Board[next].Kind == NodeKind.Goal)
            {
                goals.Arrive(ticket.EventId + "/goal", CurrentPlayer); RemainingSteps = 0;
            }
            return true;
        }
        public bool TryResolveLanding(RollTicket ticket)
        {
            if (Phase != SessionPhase.Moving || !ActiveRoll.Equals(ticket) || RemainingSteps != 0) return false;
            var kind = Board[CurrentPlayer.CurrentNodeId].Kind;
            if (kind == NodeKind.ItemPickup) Inventory.Give(ticket.EventId + "/pickup", CurrentPlayer);
            Phase = kind == NodeKind.Planning ? SessionPhase.AwaitPlan : SessionPhase.EndingTurn;
            return true;
        }
        public bool TryCompleteTurn()
        {
            if (Phase != SessionPhase.EndingTurn) return false;
            if (Players.All(p => p.Arrived)) { Phase = SessionPhase.Finished; return true; }
            Turns.EndTurn(); PinzoroBonus = 0; PendingMoveBonus = 0; PlannedSteps = 0; RemainingSteps = 0; ItemUsed = false; StepIndex = 0;
            Phase = SessionPhase.AwaitRoll; return true;
        }
        public bool TryStartSubgame(ISubgameSession session)
        {
            if (Phase != SessionPhase.AwaitPlan || session == null || session.Finished || session.PlayerId != CurrentPlayer.PlayerId) return false;
            ActiveSubgame = session; Phase = SessionPhase.RunningSubgame; return true;
        }
        public bool TryFinishSubgame(string challengeId)
        {
            if (Phase != SessionPhase.RunningSubgame || ActiveSubgame == null || ActiveSubgame.ChallengeId != challengeId || !ActiveSubgame.Finished) return false;
            // No normal-position, weekday, rank, or movement reward changes occurred in the subgame.
            ActiveSubgame = null; Phase = SessionPhase.EndingTurn; return true;
        }
        public bool TryNormalStream(int multiplierNumerator)
        {
            if (Phase != SessionPhase.AwaitPlan || !(new[] { 1, 2, 3, 6 }).Contains(multiplierNumerator)) return false;
            string id = SessionId + "/plan/" + Turns.CompletedTurns;
            Rewards.Grant(id, CurrentPlayer, RewardKind.Subscribers, checked(rules.StreamSubscribers * multiplierNumerator * (100 + CurrentPlayer.GameSkill) / 200));
            Rewards.Grant(id, CurrentPlayer, RewardKind.Funds, checked(rules.StreamFunds * multiplierNumerator / 2));
            Rewards.Grant(id, CurrentPlayer, RewardKind.GameExperience, Math.Max(1, 4 * CurrentPlayer.GameGrowthPermille * (125 - CurrentPlayer.GameSkill) / 125000));
            Phase = SessionPhase.EndingTurn; return true;
        }
        public void Cancel()
        {
            ActiveSubgame?.Cancel(); ActiveSubgame = null; Phase = SessionPhase.Cancelled;
        }
    }
}
