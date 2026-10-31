using UnityEngine;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Ending")]
    public sealed class EndingDefinition : ScriptableObject
    {
        public string endingId, title;
        [TextArea] public string text;
        public Sprite image;
        public GameObject presentationPrefab;
    }
}
