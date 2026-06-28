namespace Domain.Services
{
    public interface ISpawnPoint
    {
        int Id { get; }
        bool IsEmpty();
        int GetOccupiedByPlayer();
        void ForceRelease();
    }
}