using System;
using System.Collections.Generic;

namespace HoloDebut.Domain
{
    public enum RewardKind { Subscribers, Funds, GameExperience, StaminaExperience }
    public sealed class RewardRecord
    {
        public string SourceEventId { get; }
        public string PlayerId { get; }
        public RewardKind Kind { get; }
        public long BaseAmount { get; }
        public long AppliedAmount { get; }
        public long ModifierAmount => 0;
        public bool IsShared => false;
        public RewardRecord(string source, string player, RewardKind kind, long amount, long applied)
        { SourceEventId = source; PlayerId = player; Kind = kind; BaseAmount = amount; AppliedAmount = applied; }
    }

    public sealed class RewardService
    {
        private readonly HashSet<(string, string, RewardKind)> applied = new HashSet<(string, string, RewardKind)>();
        private readonly List<RewardRecord> history = new List<RewardRecord>();
        public IReadOnlyList<RewardRecord> History => history.AsReadOnly();
        public bool Grant(string eventId, PlayerState player, RewardKind kind, long amount)
        {
            if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException(nameof(eventId));
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var key = (eventId, player.PlayerId, kind);
            if (applied.Contains(key)) return false;
            long actual = amount;
            switch (kind)
            {
                case RewardKind.Subscribers: player.Subscribers = checked(player.Subscribers + amount); break;
                case RewardKind.Funds: player.Funds = checked(player.Funds + amount); break;
                case RewardKind.GameExperience: actual = player.Grow(SkillKind.Game, checked((int)amount)); break;
                case RewardKind.StaminaExperience: actual = player.Grow(SkillKind.Stamina, checked((int)amount)); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            applied.Add(key);
            history.Add(new RewardRecord(eventId, player.PlayerId, kind, amount, actual));
            return true;
        }
    }
}
