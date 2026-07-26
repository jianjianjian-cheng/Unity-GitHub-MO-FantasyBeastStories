using UnityEngine;
using Controllers.Character;
using Core;

namespace Controllers.Combat
{
    public interface IImpactCannon
    {
        void SetToken(AttackToken newToken);
        void SetAttributeFromPlayer(AttributePlayerBase attributePlayer);
        void StartShoot(Vector3 direction, bool isMine = true);
    }
}