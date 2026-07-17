namespace Controllers.Services
{
    public interface ISpawnPoint
    {
        int Id { get; }
        bool IsEmpty();
        int GetOccupiedByPlayer();
        void ForceRelease();
        void SetOccupied(bool occupied, int playerActorNumber);
    }
}