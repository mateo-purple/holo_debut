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
            if (players == null || players.Length < 2 || players.Length > 4) return "SessionConfigのplayersは2～4人に設定してください";
            foreach (var p in players) if (!p || string.IsNullOrEmpty(p.characterId)) return "キャラクター設定またはcharacterIdがありません";
            if (!board || !dice || !goals || !postGoal || !rocketCarrot || !endurance) return "必須の設定アセットがありません";
            if (!dicePrefab || !bowlPrefab || !pawnPrefab || !tilePrefab) return "サイコロ・皿・駒・マスのPrefabがありません";
            if (!dicePrefab.GetComponent<DiceBody>() || !bowlPrefab.GetComponent<BowlSurface>()) return "サイコロPrefabにはDiceBody、皿PrefabにはBowlSurfaceが必要です";
            if (!pawnPrefab.GetComponent<PawnVisual>() || !tilePrefab.GetComponent<TileVisual>()) return "駒PrefabにはPawnVisual、マスPrefabにはTileVisualが必要です";
            if (initialFunds < 0 || movementSubscribers < 0 || initialCarrots < 0 || inventoryCapacity < 1 || initialCarrots > inventoryCapacity) return "初期資金・初期アイテム数・所持上限の設定が不正です";
            if (stepSeconds <= 0 || stepPauseSeconds < 0 || resultSeconds < 0) return "移動・表示時間の設定が不正です";
            if (dice.size <= 0 || dice.mass <= 0 || dice.stopSeconds <= 0 || dice.timeoutSeconds <= dice.stopSeconds) return "サイコロの物理設定が不正です";
            if (rocketCarrot.itemId != HoloDebut.Domain.ItemDefinitionData.RocketCarrotId || rocketCarrot.moveBonus < 0) return "ロケットニンジンの設定が不正です";
            if (goals.arrivalBonuses == null || goals.arrivalBonuses.Length < 4) return "到着順報酬を4人分設定してください";
            foreach (long amount in goals.arrivalBonuses) if (amount < 0) return "到着順報酬は0以上にしてください";
            if (postGoal.pinzoro < 0 || postGoal.triplePerFace < 0 || postGoal.shigoro < 0 || postGoal.pair < 0 || postGoal.noRole < 0 || postGoal.hifumi < 0) return "ゴール後報酬は0以上にしてください";
            endurance.CreateRules();
            if (!streamPlan || !streamPlan.Valid) return "通常ゲーム配信の設定アセットがないか、値が不正です";
            return null;
        }
    }
}
