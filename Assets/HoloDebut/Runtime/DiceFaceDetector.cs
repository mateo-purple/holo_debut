using UnityEngine;
namespace HoloDebut.Runtime
{
    public static class DiceFaceDetector
    {
        public static readonly Vector3[] Normals = { Vector3.up, Vector3.forward, Vector3.right, Vector3.left, Vector3.back, Vector3.down };
        public static bool TryRead(Quaternion physicsRotation, float minimum, float gap, out int face, out float best, out float second)
        {
            best=-2;second=-2;face=0;
            for(int i=0;i<6;i++)
            {
                float dot=Vector3.Dot(physicsRotation*Normals[i],Vector3.up);
                if(dot>best){second=best;best=dot;face=i+1;}else if(dot>second)second=dot;
            }
            return best>=minimum && best-second>=gap;
        }
    }
}
