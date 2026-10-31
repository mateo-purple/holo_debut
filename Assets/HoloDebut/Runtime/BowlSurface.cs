using UnityEngine;
namespace HoloDebut.Runtime
{
    // Static concave interior: never use a convex collider that fills the bowl.
    public sealed class BowlSurface : MonoBehaviour
    {
        public Transform Visual { get; private set; }
        public Collider InteriorCollider { get; private set; }
        private Mesh mesh;
        private Material material;
        private PhysicsMaterial physicsMaterial;
        public void Initialize(DicePhysicsConfig config)
        {
            var collision = new GameObject("Collision"); collision.transform.SetParent(transform, false);
            const int segments = 64;
            float[] radii = { 0, 0.105f, 0.13f, 0.16f, 0.185f, 0.20f };
            float[] heights = { 0, 0, 0.007f, 0.025f, 0.065f, 0.10f };
            var vertices = new Vector3[radii.Length * segments];
            var triangles = new int[(radii.Length - 1) * segments * 6]; int index = 0;
            for (int r=0; r<radii.Length; r++) for (int s=0;s<segments;s++)
            {
                float a = 2*Mathf.PI*s/segments; vertices[r*segments+s] = new Vector3(Mathf.Cos(a)*radii[r], heights[r], Mathf.Sin(a)*radii[r]);
                if (r == radii.Length-1) continue;
                int a0=r*segments+s, b=r*segments+(s+1)%segments, c=(r+1)*segments+s, d=(r+1)*segments+(s+1)%segments;
                triangles[index++]=a0;triangles[index++]=b;triangles[index++]=c;
                triangles[index++]=b;triangles[index++]=d;triangles[index++]=c;
            }
            mesh = new Mesh { name="Temporary concave bowl" }; mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var mc=collision.AddComponent<MeshCollider>(); mc.sharedMesh=mesh;mc.convex=false;InteriorCollider=mc;
            physicsMaterial=new PhysicsMaterial("Bowl") {staticFriction=config.bowlStaticFriction,dynamicFriction=config.bowlDynamicFriction,bounciness=config.bowlBounce,frictionCombine=PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Average};
            mc.sharedMaterial=physicsMaterial;
            var visual=new GameObject("Visual");visual.transform.SetParent(transform,false);Visual=visual.transform;
            visual.AddComponent<MeshFilter>().sharedMesh=mesh; material=VisualFactory.Material(new Color(0.35f,0.19f,0.08f));visual.AddComponent<MeshRenderer>().sharedMaterial=material;
            // A table is deliberately not an eligible support collider.
            VisualFactory.Primitive(PrimitiveType.Cube,transform,"Table",new Vector3(0,-0.04f,0),new Vector3(0.6f,0.05f,0.6f),Color.gray,true);
        }
        public bool Contains(Vector3 world, float radius)
        { var p=transform.InverseTransformPoint(world); return new Vector2(p.x,p.z).sqrMagnitude<=radius*radius && p.y>=-0.01f && p.y<=0.14f; }
        private void OnDestroy() { if(mesh)Destroy(mesh);if(material)Destroy(material);if(physicsMaterial)Destroy(physicsMaterial); }
    }
}
