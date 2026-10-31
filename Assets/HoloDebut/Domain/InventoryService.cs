using System;
using System.Collections.Generic;

namespace HoloDebut.Domain
{
    // Effects are composed here, never by the UI. Future items can implement this contract.
    public interface IDiceItemEffect
    {
        int ResolveMovement(int physicalFace);
    }
    public sealed class MoveBonusEffect : IDiceItemEffect
    {
        public int Bonus { get; }
        public MoveBonusEffect(int bonus) { if (bonus < 0) throw new ArgumentOutOfRangeException(nameof(bonus)); Bonus = bonus; }
        public int ResolveMovement(int physicalFace) => checked(physicalFace + Bonus);
    }
    public sealed class ItemDefinitionData
    {
        public const string RocketCarrotId = "rocket_carrot";
        public string Id { get; }
        public IDiceItemEffect Effect { get; }
        public ItemDefinitionData(string id, IDiceItemEffect effect)
        { if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException(nameof(id)); Id = id; Effect = effect ?? throw new ArgumentNullException(nameof(effect)); }
        public static ItemDefinitionData RocketCarrot(int bonus = 5) => new ItemDefinitionData(RocketCarrotId, new MoveBonusEffect(bonus));
    }
    public sealed class InventoryService
    {
        public int Capacity { get; }
        private readonly HashSet<(string, string)> pickups = new HashSet<(string, string)>();
        public InventoryService(int capacity = 3) { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); Capacity = capacity; }
        public int Count(PlayerState player, string itemId = ItemDefinitionData.RocketCarrotId) => player.Items.TryGetValue(itemId, out int n) ? n : 0;
        public int Total(PlayerState player) { int n = 0; foreach (int count in player.Items.Values) n = checked(n + count); return n; }
        public bool Give(string eventId, PlayerState player, string itemId = ItemDefinitionData.RocketCarrotId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Missing event/item ID");
            var key = (eventId, player.PlayerId);
            if (pickups.Contains(key)) return false;
            pickups.Add(key);
            if (Total(player) >= Capacity) return false;
            player.Items[itemId] = Count(player, itemId) + 1; return true;
        }
        internal bool Consume(PlayerState player, string itemId)
        {
            int count = Count(player, itemId);
            if (count == 0) return false;
            player.Items[itemId] = count - 1; return true;
        }
    }
}
