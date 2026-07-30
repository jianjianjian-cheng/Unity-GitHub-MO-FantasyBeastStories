using Photon.Pun;
using Controllers.Network;
using UnityEngine;
using Core.SharedModel;

namespace Controllers.Network
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
      _photonView.RPC(methodName, NetworkTargetMapper.ToRpcTarget(target), args);
    }
  }
}