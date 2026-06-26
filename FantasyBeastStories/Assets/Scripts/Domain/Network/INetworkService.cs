namespace Domain.Network
{
    public interface INetworkIdentity
    {
        bool IsMine { get; }
        bool IsMasterClient { get; }
        int ViewID { get; }
    }

    public interface INetworkRPC
    {
        void RPC(string methodName, NetworkTarget target, params object[] args);
    }

    public enum NetworkTarget
    {
        All,
        Others,
        MasterClient,
        AllBuffered
    }
}
