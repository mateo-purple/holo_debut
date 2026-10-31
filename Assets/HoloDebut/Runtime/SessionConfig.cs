using UnityEngine;

namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Session")]
    public sealed class SessionConfig : ScriptableObject
    {
        public CharacterDefinition[] players;
        public bool characterPerformance = false;
        public long initialFunds = 50000;
        public long movementSubscribers = 1000;
        public int initialCarrots = 1;
        public int inventoryCapacity = 3;
        public float stepSeconds = 0.18f;
        public float stepPauseSeconds = 0.06f;
        public float resultSeconds = 0.75f;
        public BoardDefinition board;
        public DicePhysicsConfig dice;
        public GoalRewardConfig goals;
        public PostGoalRewardConfig postGoal;
        public ItemDefinition rocketCarrot;
        public EnduranceConfig endurance;
        public StreamPlanConfig streamPlan;
        public GameObject dicePrefab, bowlPrefab, pawnPrefab, tilePrefab;
        public string Validate()
        {
            if (players == null || players.Length < 2 || players.Length > 4) return "SessionConfig.players must contain 2-4 characters";
            foreach (var p in players) if (!p || string.IsNullOrEmpty(p.characterId)) return "CharacterDefinition missing/invalid";
            if (!board || !dice || !goals || !postGoal || !rocketCarrot || !endurance) return "Missing required configuration asset";
            if (!dicePrefab || !bowlPrefab || !pawnPrefab || !tilePrefab) return "Missing required dice/bowl/pawn/tile prefab";
            if (!dicePrefab.GetComponent<DiceBody>() || !bowlPrefab.GetComponent<BowlSurface>()) return "Prefab requires DiceBody/BowlSurface";
            if (!pawnPrefab.GetComponent<PawnVisual>() || !tilePrefab.GetComponent<TileVisual>()) return "Prefab requires PawnVisual/TileVisual";
            if (initialFunds < 0 || movementSubscribers < 0 || initialCarrots < 0 || inventoryCapacity < 1 || initialCarrots > inventoryCapacity) return "Invalid initial economy/inventory";
            if (stepSeconds <= 0 || stepPauseSeconds < 0 || resultSeconds < 0) return "Invalid animation durations";
            if (dice.size <= 0 || dice.mass <= 0 || dice.stopSeconds <= 0 || dice.timeoutSeconds <= dice.stopSeconds) return "Invalid dice physics configuration";
            if (rocketCarrot.itemId != HoloDebut.Domain.ItemDefinitionData.RocketCarrotId || rocketCarrot.moveBonus < 0) return "Invalid Rocket Carrot definition";
            if (goals.arrivalBonuses == null || goals.arrivalBonuses.Length < 4) return "Missing arrival rank rewards";
            foreach (long amount in goals.arrivalBonuses) if (amount < 0) return "Negative goal reward";
            if (postGoal.pinzoro < 0 || postGoal.triplePerFace < 0 || postGoal.shigoro < 0 || postGoal.pair < 0 || postGoal.noRole < 0 || postGoal.hifumi < 0) return "Negative post-goal reward";
            endurance.CreateRules();
            if (!streamPlan || !streamPlan.Valid) return "Invalid/missing StreamPlanConfig";
            return null;
        }
    }
}
