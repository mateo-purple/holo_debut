using UnityEngine;
using HoloDebut.Domain;
namespace HoloDebut.Runtime
{
    [CreateAssetMenu(menuName = "HoloDebut/Board")]
    public sealed class BoardDefinition : ScriptableObject
    {
        public int goalId = 120, columns = 11;
        public float spacing = 1.25f, tileSize = 0.85f, tileHeight = 0.15f;
        public bool enablePlanning = true;
        public int[] planningNodes = { 10, 40, 70, 100 };
        public BoardGraph CreateGraph() => BoardGraph.Prototype(enablePlanning, goalId, planningNodes);
        public Vector3 Position(int id)
        {
            int row = id / columns, col = id % columns;
            if (row % 2 == 1) col = columns - 1 - col;
            return new Vector3(col * spacing, tileHeight * 0.5f, row * spacing);
        }
    }
}
