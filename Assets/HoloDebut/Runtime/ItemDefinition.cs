using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string itemId = ItemDefinitionData.RocketCarrotId;
        public string displayName = "ロケットニンジン";
        public string description = "次の移動量＋5マス。耐久配信では追加投球も1回獲得。";
        public Sprite icon;
        public int moveBonus = 5;
        public ItemDefinitionData CreateData() => new ItemDefinitionData(itemId, new MoveBonusEffect(moveBonus));
    }
}
