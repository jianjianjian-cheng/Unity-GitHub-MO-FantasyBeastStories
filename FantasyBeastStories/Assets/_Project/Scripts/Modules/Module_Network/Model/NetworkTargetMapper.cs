using Photon.Pun;
using Core.SharedModel;

namespace Controllers.Network
{
    public static class NetworkTargetMapper
    {
        public static RpcTarget ToRpcTarget(NetworkTarget target) => target switch
        {
            NetworkTarget.All => RpcTarget.All,
            NetworkTarget.Others => RpcTarget.Others,
            NetworkTarget.MasterClient => RpcTarget.MasterClient,
            NetworkTarget.AllBuffered => RpcTarget.AllBuffered,
            _ => RpcTarget.All
        };
    }
}
