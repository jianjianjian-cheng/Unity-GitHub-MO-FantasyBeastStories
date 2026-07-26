namespace Core.Contracts
{
    public interface IGameActionService
    {
        void QuitToMainMenu();
        void SetLocalReady(bool ready);
    }
}