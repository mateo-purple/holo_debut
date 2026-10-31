using System;
using System.Collections.Generic;

namespace HoloDebut.Domain
{
    public enum SkillKind { Game, Chat, Song, Asmr, Stamina }

    [Serializable]
    public sealed class PlayerState
    {
        public string PlayerId { get; }
        public string CharacterId { get; }
        public string DisplayName { get; }
        public int CurrentNodeId { get; internal set; }
        public long Subscribers { get; internal set; }
        public long Funds { get; internal set; }
        public bool Arrived { get; internal set; }
        public int ArrivalRank { get; internal set; }
        private readonly int[] skills = { 40, 40, 40, 40, 50 };
        internal readonly Dictionary<string, int> Items = new Dictionary<string, int>();
        public int GameSkill => Skill(SkillKind.Game);
        public int Stamina => Skill(SkillKind.Stamina);
        public int GameGrowthPermille { get; private set; } = 1200;
        public void SetGameGrowthPermille(int value)
        { if (value < 0 || value > 4000) throw new ArgumentOutOfRangeException(nameof(value)); GameGrowthPermille = value; }

        public PlayerState(string playerId, string characterId, string displayName, long initialFunds = 50000)
        {
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException(nameof(playerId));
            if (initialFunds < 0) throw new ArgumentOutOfRangeException(nameof(initialFunds));
            PlayerId = playerId; CharacterId = characterId; DisplayName = displayName; Funds = initialFunds;
        }
        public int Skill(SkillKind kind) => skills[(int)kind];
        public void SetSkill(SkillKind kind, int value)
        {
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
            skills[(int)kind] = value;
        }
        internal int Grow(SkillKind kind, int experience)
        {
            if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
            int before = Skill(kind);
            skills[(int)kind] = (int)Math.Min(100L, (long)before + experience);
            return skills[(int)kind] - before;
        }
    }
}
