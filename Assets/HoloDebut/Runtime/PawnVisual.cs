using UnityEngine;
namespace HoloDebut.Runtime
{
    public sealed class PawnVisual : MonoBehaviour
    {
        public void Initialize(Color color,string label)
        {
            VisualFactory.Primitive(PrimitiveType.Cylinder,transform,"Visual",new Vector3(0,0.14f,0),new Vector3(0.24f,0.14f,0.24f),color);
            var t=VisualFactory.Label(transform,label,new Vector3(0,0.45f,0),0.045f,color);t.transform.localRotation=Quaternion.Euler(45,0,0);
        }
    }
}
