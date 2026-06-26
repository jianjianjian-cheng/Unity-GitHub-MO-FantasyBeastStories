using Photon.Pun;
using Domain.Network;
using UnityEngine;

namespace Infrastructure.Network
{
  public class PhotonNetworkAdapter : NetworkIdentityBase
  {
    private PhotonView _photonView;

    private void Awake()
    {
      _photonView = GetComponent<PhotonView>();
    }

    public override bool IsMine => _photonView != null && _photonView.IsMine;
    public override bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public override int ViewID => _photonView != null ? _photonView.ViewID : -1;

    public override void RPC(string methodName, NetworkTarget target, params object[] args)
    {
      RpcTarget photonTarget = target switch
      {
        NetworkTarget.All => RpcTarget.All,
        NetworkTarget.Others => RpcTarget.Others,
        NetworkTarget.MasterClient => RpcTarget.MasterClient,
        NetworkTarget.AllBuffered => RpcTarget.AllBuffered,
        _ => RpcTarget.All
      };
      _photonView.RPC(methodName, photonTarget, args);
    }
  }
}