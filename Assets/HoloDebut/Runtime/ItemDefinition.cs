using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string itemId = ItemDefinitionData.RocketCarrotId;
        public string displayName = "Rocket Carrot";
        public string description = "Next movement +5. In endurance: also adds one roll.";
        public Sprite icon;
        public int moveBonus = 5;
        public ItemDefinitionData CreateData() => new ItemDefinitionData(itemId, new MoveBonusEffect(moveBonus));
    }
}
