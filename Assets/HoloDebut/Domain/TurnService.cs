using System;
using System.Collections.Generic;
using System.Linq;

namespace HoloDebut.Domain
{
    public sealed class TurnService
    {
        public int PlayerCount { get; }
        public long CompletedTurns { get; private set; }
        public int CurrentPlayerIndex => (int)(CompletedTurns % PlayerCount);
        public int DayIndex => (int)((CompletedTurns / PlayerCount) % 7);
        public long Week => CompletedTurns / (PlayerCount * 7) + 1;
        public bool Weekend => DayIndex >= 5;
        public TurnService(int playerCount)
        { if (playerCount < 2 || playerCount > 4) throw new ArgumentOutOfRangeException(nameof(playerCount)); PlayerCount = playerCount; }
        internal void EndTurn() { CompletedTurns = checked(CompletedTurns + 1); }
    }
    public sealed class GoalService
    {
        private readonly long[] arrivalBonuses;
        private readonly RewardService rewards;
        public int ArrivalCount { get; private set; }
        public GoalService(RewardService rewards, long[] bonuses)
        { this.rewards = rewards; arrivalBonuses = (long[])bonuses.Clone(); if (bonuses.Length < 4 || bonuses.Any(x => x < 0)) throw new ArgumentException(nameof(bonuses)); }
        public bool Arrive(string eventId, PlayerState p)
        {
            if (p.Arrived) return false;
            rewards.Grant(eventId, p, RewardKind.Subscribers, arrivalBonuses[ArrivalCount]);
            p.Arrived = true; p.ArrivalRank = ++ArrivalCount; return true;
        }
    }
    public sealed class RankedPlayer
    {
        public PlayerState Player { get; }
        public int Rank { get; }
        public RankedPlayer(PlayerState player, int rank) { Player = player; Rank = rank; }
    }
    public static class EndingService
    {
        public static IReadOnlyList<RankedPlayer> Rank(IEnumerable<PlayerState> players)
        {
            var sorted = players.OrderByDescending(p => p.Subscribers).ToArray();
            var list = new List<RankedPlayer>(); int rank = 1;
            for (int i = 0; i < sorted.Length; i++)
            { if (i > 0 && sorted[i].Subscribers != sorted[i - 1].Subscribers) rank = i + 1; list.Add(new RankedPlayer(sorted[i], rank)); }
            return list.AsReadOnly();
        }
    }
}
