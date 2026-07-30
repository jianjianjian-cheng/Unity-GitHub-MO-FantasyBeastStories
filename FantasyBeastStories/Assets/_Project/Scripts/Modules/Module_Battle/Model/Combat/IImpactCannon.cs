using UnityEngine;
using Controllers.Character;
using Core;
using Core.SharedModel;

namespace Controllers.Battle
{
    public interface IImpactCannon
    {
        void SetToken(AttackToken newToken);
        void SetAttributeFromPlayer(AttributePlayerBase attributePlayer);
        void StartShoot(Vector3 direction, bool isMine = true);
    }
}