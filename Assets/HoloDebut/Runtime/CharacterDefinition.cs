using UnityEngine;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string characterId, japaneseName, englishName, favoriteGenre;
        public Color color = Color.white;
        public Sprite portrait;
        public GameObject pawnPrefab;
        public int game = 40, chat = 40, song = 40, asmr = 40, stamina = 50;
        public int gameGrowthPermille = 1200;
        public EndingDefinition ending;
    }
}
