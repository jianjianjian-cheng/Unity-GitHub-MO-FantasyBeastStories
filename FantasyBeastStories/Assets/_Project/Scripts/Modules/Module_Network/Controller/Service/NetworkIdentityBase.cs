using UnityEngine;
using Core.SharedModel;

namespace Controllers.Network
{
    public abstract class NetworkIdentityBase : MonoBehaviour, INetworkIdentity, INetworkRPC
    {
        public abstract bool IsMine { get; }
        public abstract bool IsMasterClient { get; }
        public abstract int ViewID { get; }
        public abstract void RPC(string methodName, NetworkTarget target, params object[] args);
    }
}