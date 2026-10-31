using System;
using System.Collections.Generic;

namespace HoloDebut.Domain
{
    public enum NodeKind { Start, Empty, ItemPickup, Goal, Planning }
    public sealed class BoardNode
    {
        public int Id { get; }
        public NodeKind Kind { get; }
        public IReadOnlyList<int> NextIds { get; }
        public string CheckpointId { get; }
        public BoardNode(int id, NodeKind kind, int[] next, string checkpoint = null)
        { Id = id; Kind = kind; NextIds = Array.AsReadOnly((int[])(next ?? throw new ArgumentNullException(nameof(next))).Clone()); CheckpointId = checkpoint; }
    }
    public sealed class BoardGraph
    {
        private readonly Dictionary<int, BoardNode> nodes = new Dictionary<int, BoardNode>();
        public IReadOnlyCollection<BoardNode> Nodes => nodes.Values;
        public int StartId { get; }
        public BoardNode this[int id] => nodes[id];
        public BoardGraph(IEnumerable<BoardNode> source, int startId = 0)
        {
            StartId = startId;
            foreach (var node in source)
            { if (nodes.ContainsKey(node.Id)) throw new ArgumentException("Duplicate node ID"); nodes.Add(node.Id, node); }
            if (!nodes.ContainsKey(startId)) throw new ArgumentException("Missing start node");
            foreach (var n in nodes.Values) foreach (int to in n.NextIds) if (!nodes.ContainsKey(to)) throw new ArgumentException("Unknown connection");
            var visiting = new HashSet<int>(); var done = new HashSet<int>();
            Visit(startId, visiting, done);
            if (done.Count != nodes.Count) throw new ArgumentException("Unreachable node");
        }
        private void Visit(int id, HashSet<int> visiting, HashSet<int> done)
        {
            if (done.Contains(id)) return;
            if (!visiting.Add(id)) throw new ArgumentException("Cycle in normal board");
            var n = nodes[id];
            if (n.Kind == NodeKind.Goal && n.NextIds.Count != 0) throw new ArgumentException("Goal has an outgoing edge");
            if (n.Kind != NodeKind.Goal && n.NextIds.Count == 0) throw new ArgumentException("Dead end before goal");
            foreach (int next in n.NextIds) Visit(next, visiting, done);
            visiting.Remove(id); done.Add(id);
        }
        public static BoardGraph Prototype(bool enablePlanning = true, int goal = 120, int[] planningNodes = null)
        {
            if (goal < 1) throw new ArgumentOutOfRangeException(nameof(goal));
            var planning = new HashSet<int>(planningNodes ?? new[] { 10, 40, 70, 100 });
            var list = new List<BoardNode>();
            for (int i = 0; i <= goal; i++)
            {
                var kind = i == 0 ? NodeKind.Start : i == goal ? NodeKind.Goal :
                    (i == 20 || i == 50 || i == 80 || i == 110) ? NodeKind.ItemPickup :
                    enablePlanning && planning.Contains(i) ? NodeKind.Planning : NodeKind.Empty;
                list.Add(new BoardNode(i, kind, i == goal ? Array.Empty<int>() : new[] { i + 1 },
                    i == 30 || i == 60 || i == 90 ? "collab_" + i : null));
            }
            return new BoardGraph(list);
        }
    }
}
