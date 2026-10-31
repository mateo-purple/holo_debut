using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    public sealed class EnduranceBoardNode
    {
        public int Number {get;}public Vector3 Position {get;}public RewardKind Kind {get;}public long Reward {get;}
        public EnduranceBoardNode(int number,Vector3 position,RewardKind kind,long reward){Number=number;Position=position;Kind=kind;Reward=reward;}
    }
    public sealed class EnduranceBoardGenerator : MonoBehaviour
    {
        public EnduranceBoardNode[] Nodes {get;private set;}
        public void Generate(int count,EnduranceConfig config,GameObject tilePrefab)
        {
            if(count<2||count>12||config.radius<=0)throw new System.ArgumentOutOfRangeException(nameof(count));
            Nodes=new EnduranceBoardNode[count];var rules=config.CreateRules();
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;Vector3 position=new Vector3(Mathf.Sin(a)*config.radius,0,Mathf.Cos(a)*config.radius);
                var reward=EnduranceRewardResolver.Resolve(rules,i,1);Nodes[i]=new EnduranceBoardNode(i,position,reward.Kind,reward.Amount);
                var tile=Instantiate(tilePrefab,transform);tile.name="EnduranceNode_"+i;tile.transform.localPosition=position;
                tile.GetComponent<TileVisual>().Initialize(i+(i==0?"\nEXIT":i%2==1?"\nXP":"\nFUNDS"),i==0?Color.yellow:i%2==1?new Color(0.8f,0.5f,1):new Color(0.4f,0.9f,0.6f),0.7f,0.15f);
            }
        }
        public Vector3 Anchor(int position)=>transform.TransformPoint(Nodes[position].Position)+Vector3.up*0.1f;
    }
}
