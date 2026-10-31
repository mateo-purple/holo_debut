namespace HoloDebut.Domain
{
    public interface ISubgameSession
    {
        string ChallengeId { get; }
        string PlayerId { get; }
        bool Finished { get; }
        void Cancel();
    }
}
