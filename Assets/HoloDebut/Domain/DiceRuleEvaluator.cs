using System;
using System.Collections.Generic;

namespace HoloDebut.Domain
{
    public enum DiceRole { Cho, Han, Pinzoro, Triple, Shigoro, Hifumi, Pair, NoRole }
    public sealed class DiceResult
    {
        private readonly int[] faces;
        public IReadOnlyList<int> RawFaces => Array.AsReadOnly(faces);
        public DiceRole Role { get; }
        public int BaseMoveSteps { get; }
        public bool RequiresReroll => Role == DiceRole.Pinzoro;
        public int BonusStepsToAdd => RequiresReroll ? 3 : 0;
        public int TripleFace => Role == DiceRole.Triple ? faces[0] : 0;
        public DiceResult(int[] raw, DiceRole role, int steps) { faces = (int[])raw.Clone(); Role = role; BaseMoveSteps = steps; }
    }
    public static class DiceRuleEvaluator
    {
        public static DiceResult Evaluate(int[] faces, bool chinchiro)
        {
            if (faces == null) throw new ArgumentNullException(nameof(faces));
            if (faces.Length != (chinchiro ? 3 : 2)) throw new ArgumentException("Incorrect dice count");
            foreach (int f in faces) if (f < 1 || f > 6) throw new ArgumentOutOfRangeException(nameof(faces));
            int[] s = (int[])faces.Clone(); Array.Sort(s);
            if (!chinchiro) { int sum = s[0] + s[1]; return new DiceResult(faces, sum % 2 == 0 ? DiceRole.Cho : DiceRole.Han, sum); }
            DiceRole role; int move;
            if (s[0] == s[2]) { role = s[0] == 1 ? DiceRole.Pinzoro : DiceRole.Triple; move = s[0] == 1 ? 0 : s[0] * 3; }
            else if (s[0] == 4 && s[1] == 5 && s[2] == 6) { role = DiceRole.Shigoro; move = 11; }
            else if (s[0] == 1 && s[1] == 2 && s[2] == 3) { role = DiceRole.Hifumi; move = 1; }
            else if (s[0] == s[1]) { role = DiceRole.Pair; move = s[0] + s[2]; }
            else if (s[1] == s[2]) { role = DiceRole.Pair; move = s[0] + s[1]; }
            else { role = DiceRole.NoRole; move = s[1] + s[2]; }
            return new DiceResult(faces, role, move);
        }
    }
    public static class MovementResultResolver
    {
        public static long Resolve(DiceResult result, long pinzoroBonus, long itemBonus)
        {
            if (result == null || result.RequiresReroll || pinzoroBonus < 0 || itemBonus < 0) throw new ArgumentException("Movement is not final");
            return checked(result.BaseMoveSteps + checked(pinzoroBonus + itemBonus));
        }
    }
    public sealed class PostGoalRewardRules
    {
        public long Pinzoro = 30000, TriplePerFace = 3000, Shigoro = 20000, Pair = 5000, NoRole = 2000, Hifumi = 0;
        public long Evaluate(DiceResult r)
        {
            switch (r.Role)
            {
                case DiceRole.Pinzoro: return Pinzoro;
                case DiceRole.Triple: return checked(r.TripleFace * TriplePerFace);
                case DiceRole.Shigoro: return Shigoro;
                case DiceRole.Pair: return Pair;
                case DiceRole.NoRole: return NoRole;
                case DiceRole.Hifumi: return Hifumi;
                default: throw new ArgumentException("Post-goal requires three dice");
            }
        }
    }
}
