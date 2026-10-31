using System.Collections.Generic;
using UnityEngine;
namespace HoloDebut.Runtime
{
    public sealed class DiceBody : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public bool HasSupport => supports.Count>0;
        public float StillSeconds { get; private set; }
        public float BestDot { get; private set; }
        public float SecondDot { get; private set; }
        public float UnsupportedStillSeconds { get; private set; }
        public string InvalidSupportReason
        { get { foreach(var reason in invalidSupports.Values)return reason;return null; } }
        private readonly HashSet<Collider> supports=new HashSet<Collider>();
        private readonly Dictionary<Collider,string> invalidSupports=new Dictionary<Collider,string>();
        private BowlSurface bowl;
        private DicePhysicsConfig config;
        private PhysicsMaterial material;
        public void Initialize(DicePhysicsConfig physicsConfig, BowlSurface surface)
        {
            config=physicsConfig;bowl=surface;
            var box=gameObject.AddComponent<BoxCollider>();box.size=Vector3.one*config.size;box.contactOffset=config.contactOffset;
            material=new PhysicsMaterial("Dice") {staticFriction=config.diceStaticFriction,dynamicFriction=config.diceDynamicFriction,bounciness=config.diceBounce,frictionCombine=PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Average};box.sharedMaterial=material;
            Body=gameObject.AddComponent<Rigidbody>();Body.mass=config.mass;Body.linearDamping=config.linearDamping;Body.angularDamping=config.angularDamping;
            Body.maxAngularVelocity=config.maxAngularVelocity;Body.solverIterations=config.solverIterations;Body.solverVelocityIterations=config.solverVelocityIterations;
            Body.collisionDetectionMode=CollisionDetectionMode.Discrete;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.useGravity=true;Body.isKinematic=true;
            VisualFactory.Primitive(PrimitiveType.Cube,transform,"Visual",Vector3.zero,Vector3.one*config.size,new Color(0.96f,0.96f,0.90f));
            for(int i=0;i<6;i++)
            {
                Vector3 normal=DiceFaceDetector.Normals[i];var t=VisualFactory.Label(transform,(i+1).ToString(),normal*config.size*0.505f,config.size*0.12f,i==0?Color.red:Color.black);
                t.transform.localRotation=Quaternion.LookRotation(-normal,Mathf.Abs(normal.y)>0.9f?Vector3.forward:Vector3.up);
            }
        }
        public void Prepare(Vector3 position, Quaternion rotation)
        {
            Body.isKinematic=false;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;
            Body.position=position;Body.rotation=rotation;Body.collisionDetectionMode=CollisionDetectionMode.Discrete;Body.isKinematic=true;supports.Clear();invalidSupports.Clear();StillSeconds=0;UnsupportedStillSeconds=0;
        }
        public void Launch(Vector3 velocity,Vector3 angular)
        { Body.isKinematic=false;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.linearVelocity=velocity;Body.angularVelocity=angular;Body.WakeUp(); }
        public bool Tick(float delta)
        {
            bool still=Body.linearVelocity.sqrMagnitude<config.linearStop*config.linearStop && Body.angularVelocity.sqrMagnitude<config.angularStop*config.angularStop;
            StillSeconds=still && HasSupport?StillSeconds+delta:0;
            UnsupportedStillSeconds=still&&!HasSupport&&invalidSupports.Count>0?UnsupportedStillSeconds+delta:0;
            return StillSeconds>=config.stopSeconds;
        }
        public bool TryFace(out int face)
        {
            bool ok=DiceFaceDetector.TryRead(Body.rotation,config.minUpDot,config.minDotGap,out face,out float best,out float second);
            BestDot=best;SecondDot=second;return ok;
        }
        public void Freeze() { if(Body){Body.collisionDetectionMode=CollisionDetectionMode.Discrete;Body.isKinematic=true;} }
        private void OnCollisionEnter(Collision collision)=>Support(collision);
        private void OnCollisionStay(Collision collision)=>Support(collision);
        private void OnCollisionExit(Collision collision){supports.Remove(collision.collider);invalidSupports.Remove(collision.collider);}
        private void Support(Collision c)
        {
            if(!bowl)return;
            bool valid=false;
            for(int i=0;i<c.contactCount;i++)
            {var p=c.GetContact(i);if(p.point.y<Body.position.y && p.normal.y>0.25f)valid=true;}
            if(c.collider==bowl.InteriorCollider)
            {if(valid)supports.Add(c.collider);else supports.Remove(c.collider);}
            else if(valid)invalidSupports[c.collider]=c.collider.GetComponentInParent<DiceBody>()?"Stacked on another die":"Landed on table/floor";
            else invalidSupports.Remove(c.collider);
        }
        private void OnDisable(){supports.Clear();invalidSupports.Clear();StillSeconds=0;UnsupportedStillSeconds=0;Freeze();}
        private void OnDestroy(){if(material)Destroy(material);}
    }
}
