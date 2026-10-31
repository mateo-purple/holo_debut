using UnityEngine;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Goal Rewards")]
    public sealed class GoalRewardConfig : ScriptableObject
    { public long[] arrivalBonuses = {100000,60000,30000,10000}; }
}
