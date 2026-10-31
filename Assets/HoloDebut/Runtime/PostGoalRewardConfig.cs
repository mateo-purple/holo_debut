using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Post Goal Rewards")]
    public sealed class PostGoalRewardConfig : ScriptableObject
    {
        public long pinzoro = 30000, triplePerFace = 3000, shigoro = 20000, pair = 5000, noRole = 2000, hifumi;
        public PostGoalRewardRules CreateRules() => new PostGoalRewardRules { Pinzoro=pinzoro, TriplePerFace=triplePerFace, Shigoro=shigoro, Pair=pair, NoRole=noRole, Hifumi=hifumi };
    }
}
