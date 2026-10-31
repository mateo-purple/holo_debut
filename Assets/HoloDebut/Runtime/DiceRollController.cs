using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloDebut.Domain;
using HoloDebut.Presentation;
namespace HoloDebut.Runtime
{
    public sealed class RollObservation
    {
        public RollTicket Ticket; public int AttemptIndex; public bool Valid; public string Reason; public float Seconds; public int[] Faces;
    }
    public sealed class DiceRollController : MonoBehaviour
    {
        public IReadOnlyList<DiceBody> Dice=>dice.AsReadOnly();
        public IReadOnlyList<RollObservation> History=>history.AsReadOnly();
        public int LaunchSeed {get;}=Environment.TickCount;
        public bool ErrorPaused {get;private set;}
        public string Status {get;private set;}="";
        private readonly List<DiceBody> dice=new List<DiceBody>();
        private readonly List<RollObservation> history=new List<RollObservation>();
        private System.Random launchRandom;
        private DicePhysicsConfig config;private BowlSurface bowl;private int generation;private bool busy;
        private Vector3 RandomVector(float min,float max)=>new Vector3(Range(min,max),Range(min,max),Range(min,max));
        private float Range(float min,float max)=>min+(float)launchRandom.NextDouble()*(max-min);
        private Vector3 UnitVector()
        {float z=Range(-1,1),a=Range(0,Mathf.PI*2),r=Mathf.Sqrt(1-z*z);return new Vector3(r*Mathf.Cos(a),z,r*Mathf.Sin(a));}
        private Quaternion UniformRotation()
        {float a=Range(0,1),b=Range(0,Mathf.PI*2),c=Range(0,Mathf.PI*2);return new Quaternion(Mathf.Sqrt(1-a)*Mathf.Sin(b),Mathf.Sqrt(1-a)*Mathf.Cos(b),Mathf.Sqrt(a)*Mathf.Sin(c),Mathf.Sqrt(a)*Mathf.Cos(c));}
        public void Initialize(SessionConfig session,BowlSurface surface)
        {
            config=session.dice;bowl=surface;
            launchRandom=new System.Random(LaunchSeed);
            if(config.spawnPoints==null || config.spawnPoints.Length<3 || config.maxInvalidAttempts<1)throw new InvalidOperationException("サイコロの開始位置・無効投球の上限設定が不正です");
            for(int i=0;i<3;i++){var o=Instantiate(session.dicePrefab,transform);o.name="Die"+(i+1);var body=o.GetComponent<DiceBody>();body.Initialize(config,bowl);dice.Add(body);o.SetActive(false);}
        }
        public IEnumerator Roll(RollTicket ticket,Func<bool> active,Action<int[]> completed)
        {
            if(busy || ticket.DiceCount<1 || ticket.DiceCount>3)yield break;
            busy=true;int run=++generation;ErrorPaused=false;
            try
            {
                int invalid=0,attempt=0;
                while(run==generation && active())
                {
                    if(ErrorPaused){yield return null;continue;}
                    attempt++;string reason=null;
                    var positions=new Vector3[ticket.DiceCount];
                    for(int i=0;i<dice.Count;i++)
                    {
                        dice[i].gameObject.SetActive(i<ticket.DiceCount);if(i>=ticket.DiceCount)continue;
                        positions[i]=bowl.transform.TransformPoint(config.spawnPoints[i]+RandomVector(-config.spawnJitter,config.spawnJitter));
                        for(int j=0;j<i;j++)if(Vector3.Distance(positions[i],positions[j])<config.size*1.75f)reason="Overlapping spawn points";
                        if(Vector3.Distance(positions[i],bowl.transform.position)<0.12f)reason="Spawn intersects bowl";
                        dice[i].Prepare(positions[i],UniformRotation());
                    }
                    Status="サイコロ "+ticket.DiceCount+"個を投球中（試行 "+attempt+"回目）";
                    yield return new WaitForFixedUpdate();
                    if(run!=generation || !active())yield break;
                    if(reason==null)
                    {
                        for(int i=0;i<ticket.DiceCount;i++)
                        {
                            var local=bowl.transform.InverseTransformPoint(positions[i]);var toward=new Vector3(-local.x,0,-local.z).normalized;
                            var velocity=toward*Range(config.horizontalSpeed.x,config.horizontalSpeed.y)+new Vector3(Range(-config.lateralJitter,config.lateralJitter),-Range(config.downwardSpeed.x,config.downwardSpeed.y),Range(-config.lateralJitter,config.lateralJitter));
                            dice[i].Launch(bowl.transform.TransformDirection(velocity),UnitVector()*Range(config.spinSpeed.x,config.spinSpeed.y));
                        }
                    }
                    float elapsed=0;int[] faces=null;
                    while(reason==null && elapsed<config.timeoutSeconds)
                    {
                        yield return new WaitForFixedUpdate();
                        if(run!=generation || !active())yield break;
                        elapsed+=Time.fixedDeltaTime;bool all=true;
                        for(int i=0;i<ticket.DiceCount;i++)
                        {
                            var body=dice[i];var p=bowl.transform.InverseTransformPoint(body.Body.position);
                            var q=body.Body.rotation;
                            if(!Finite(body.Body.position)||!Finite(body.Body.linearVelocity)||!Finite(body.Body.angularVelocity)||!Finite(new Vector3(q.x,q.y,q.z))||float.IsNaN(q.w)||float.IsInfinity(q.w)){reason="Non-finite physics state";break;}
                            if(new Vector2(p.x,p.z).magnitude>config.flightRadius||p.y<config.flightMinY){reason="Out of flight bounds";break;}
                            all=body.Tick(Time.fixedDeltaTime)&&all;
                            if(body.UnsupportedStillSeconds>=config.stopSeconds){reason=body.InvalidSupportReason;break;}
                        }
                        if(reason!=null)break;
                        if(all)
                        {
                            faces=new int[ticket.DiceCount];
                            for(int i=0;i<ticket.DiceCount;i++)
                            {
                                if(!bowl.Contains(dice[i].Body.position,config.interiorRadius)){reason="Stopped outside bowl interior";break;}
                                if(!dice[i].TryFace(out faces[i])){reason="Ambiguous upper face";break;}
                            }
                            break;
                        }
                    }
                    foreach(var d in dice)d.Freeze();
                    if(reason==null && faces==null)reason="Timeout (unsupported/stacked/unsettled die)";
                    var observation=new RollObservation {Ticket=ticket,AttemptIndex=attempt,Valid=reason==null,Reason=reason,Seconds=elapsed,Faces=faces==null?null:(int[])faces.Clone()};history.Add(observation);
                    if(reason==null){Status="出目："+string.Join("・",faces);completed((int[])faces.Clone());yield break;}
                    Status="無効投球："+JapaneseText.InvalidRollReason(reason);
                    yield return new WaitForSeconds(config.invalidMessageSeconds);
                    if(++invalid>=config.maxInvalidAttempts){ErrorPaused=true;Status+="\n一時停止中。「同じ投球を再試行」を押してください。";invalid=0;}
                }
            }
            finally{if(run==generation){busy=false;foreach(var d in dice)d.Freeze();}}
        }
        public bool Retry(){if(!ErrorPaused)return false;ErrorPaused=false;return true;}
        public void Cancel(){generation++;StopAllCoroutines();busy=false;ErrorPaused=false;foreach(var d in dice)d.Freeze();}
        private static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
    }
}
