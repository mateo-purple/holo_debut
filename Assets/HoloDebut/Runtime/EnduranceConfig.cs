using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName="HoloDebut/Endurance")]
    public sealed class EnduranceConfig : ScriptableObject
    {
        public int[] maximumSkills={9,19,29,39,59,69,79,94,100};
        public int[] loopSizes={12,11,10,8,6,5,4,3,2};
        public int skillExperience=5;
        public long fundsReward=10000;
        public long[] successBonuses={50000,35000,25000,15000,10000};
        public float radius=2.5f;
        public EnduranceRules CreateRules()=>new EnduranceRules(maximumSkills,loopSizes,skillExperience,fundsReward,successBonuses);
    }
}
