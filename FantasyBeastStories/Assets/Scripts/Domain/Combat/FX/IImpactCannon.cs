using UnityEngine;
using Domain.Character.Attribute;
using Domain.Data;

namespace Domain.Combat.FX
{
    public interface IImpactCannon
    {
        void SetToken(AttackToken newToken);
        void SetAttributeFromPlayer(AttributePlayerBase attributePlayer);
        void StartShoot(Vector3 direction, bool isMine = true);
    }
}