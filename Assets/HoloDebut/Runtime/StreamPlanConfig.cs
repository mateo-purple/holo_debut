using UnityEngine;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName="HoloDebut/Stream Plan")]
    public sealed class StreamPlanConfig : ScriptableObject
    {
        public long baseSubscribers=5000,baseFunds=5000;
        public int poorPercent=15,normalPercent=60,goodPercent=20,viralPercent=5;
        public int commonGrowthPermille=1200;
        public int MultiplierNumerator(int percentile)
        {return percentile<poorPercent?1:percentile<poorPercent+normalPercent?2:percentile<poorPercent+normalPercent+goodPercent?3:6;}
        public bool Valid=>baseSubscribers>=0&&baseFunds>=0&&poorPercent>=0&&normalPercent>=0&&goodPercent>=0&&viralPercent>=0&&poorPercent+normalPercent+goodPercent+viralPercent==100&&commonGrowthPermille>=0&&commonGrowthPermille<=4000;
    }
}
