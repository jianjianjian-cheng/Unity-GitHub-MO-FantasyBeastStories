using Domain.Data;
using UnityEngine;

namespace Domain.Network
{
    public interface INetworkFireballCaster
    {
        void RequestFireball(Vector3 spawnPos, Vector3 direction, float speed, Element element);
    }
}