using Domain.Event.Channels.Base;
using UnityEngine;

namespace Domain.Event.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Enemy Report Event Channel")]
    public class EnemyReportEventChannelSO : BaseEventChannelSO<EnemyReportData>
    {
    }

    public class EnemyReportData : EventArgsBase
    {
        public Vector3 position;
        public int photonViewID;

        public EnemyReportData(Vector3 position, int photonViewID)
        {
            this.position = position;
            this.photonViewID = photonViewID;
        }
    }
}
