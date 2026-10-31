namespace HoloDebut.Domain
{
    // Uses realtime supplied by the host. Holding across a turn boundary cannot start a roll.
    public sealed class ProgressInput
    {
        private bool held;
        private double since;
        public bool Press(double realtime)
        {
            if (held) return false;
            held = true; since = realtime; return true;
        }
        public bool FastForward(double realtime) => held && realtime - since >= 0.2;
        public void Release() { held = false; }
    }
}
