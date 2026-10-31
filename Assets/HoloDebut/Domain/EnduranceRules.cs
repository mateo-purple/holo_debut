using System;
using System.Linq;
namespace HoloDebut.Domain
{
    public sealed class EnduranceRules
    {
        private readonly int[] maximumSkills, loopSizes;
        private readonly long[] successBonuses;
        public int SkillExperience {get;}
        public long FundsReward {get;}
        public EnduranceRules(int[] maxima=null,int[] sizes=null,int experience=5,long funds=10000,long[] success=null)
        {
            maximumSkills=(int[])(maxima??new[]{9,19,29,39,59,69,79,94,100}).Clone();
            loopSizes=(int[])(sizes??new[]{12,11,10,8,6,5,4,3,2}).Clone();
            successBonuses=(long[])(success??new long[]{50000,35000,25000,15000,10000}).Clone();
            if(maximumSkills.Length==0||maximumSkills.Length!=loopSizes.Length||maximumSkills[maximumSkills.Length-1]!=100||experience<0||funds<0||successBonuses.Length==0||successBonuses.Any(x=>x<0))throw new ArgumentException("Invalid endurance rewards/tiers");
            for(int i=0;i<maximumSkills.Length;i++)
            {
                int min=i==0?0:maximumSkills[i-1]+1;
                if(maximumSkills[i]<min||maximumSkills[i]>100||loopSizes[i]<2||loopSizes[i]>12||(loopSizes[i]==2&&min<95))throw new ArgumentException("Invalid skill boundary/loop size (2 nodes requires 95+)");
            }
            SkillExperience=experience;FundsReward=funds;
        }
        public int LoopSize(int skill)
        {ValidateAbility(skill);for(int i=0;i<maximumSkills.Length;i++)if(skill<=maximumSkills[i])return loopSizes[i];throw new InvalidOperationException();}
        public static int BasicRolls(int stamina){ValidateAbility(stamina);return Math.Min(5,stamina/20+1);}
        public long SuccessBonus(int validThrowNumber)
        {if(validThrowNumber<1)throw new ArgumentOutOfRangeException(nameof(validThrowNumber));return successBonuses[Math.Min(validThrowNumber,successBonuses.Length)-1];}
        private static void ValidateAbility(int value){if(value<0||value>100)throw new ArgumentOutOfRangeException(nameof(value));}
    }
    public readonly struct EnduranceLandingReward
    {
        public readonly RewardKind Kind; public readonly long Amount;
        public EnduranceLandingReward(RewardKind kind,long amount){Kind=kind;Amount=amount;}
    }
    public static class EnduranceRewardResolver
    {
        public static EnduranceLandingReward Resolve(EnduranceRules rules,int position,int validThrowNumber)
        {
            if(position<0||position>11)throw new ArgumentOutOfRangeException(nameof(position));
            return position==0?new EnduranceLandingReward(RewardKind.Subscribers,rules.SuccessBonus(validThrowNumber)):
                position%2==1?new EnduranceLandingReward(RewardKind.GameExperience,rules.SkillExperience):new EnduranceLandingReward(RewardKind.Funds,rules.FundsReward);
        }
    }
}
