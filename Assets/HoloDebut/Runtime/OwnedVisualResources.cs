using UnityEngine;
namespace HoloDebut.Runtime
{
    public sealed class OwnedVisualResources : MonoBehaviour
    {
        public Material material;
        private void OnDestroy(){if(material)Destroy(material);}
    }
}
