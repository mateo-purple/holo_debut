using UnityEngine;
namespace HoloDebut.Runtime
{
    public sealed class TileVisual : MonoBehaviour
    {
        public void Initialize(string label,Color color,float size,float height)
        {
            VisualFactory.Primitive(PrimitiveType.Cube,transform,"Visual",Vector3.zero,new Vector3(size,height,size),color);
            var t=VisualFactory.Label(transform,label,new Vector3(0,height*0.5f+0.015f,0),0.055f,Color.black);t.transform.localRotation=Quaternion.Euler(90,0,0);
        }
    }
}
