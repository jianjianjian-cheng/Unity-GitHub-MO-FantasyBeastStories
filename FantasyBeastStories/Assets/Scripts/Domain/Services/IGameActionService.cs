namespace Domain.Services
{
    public interface IGameActionService
    {
        void QuitToMainMenu();
        void SetLocalReady(bool ready);
    }
}