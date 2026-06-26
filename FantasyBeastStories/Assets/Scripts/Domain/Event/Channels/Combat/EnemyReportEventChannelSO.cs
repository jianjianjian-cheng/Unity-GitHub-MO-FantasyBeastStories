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
        public int networkViewID;

        public EnemyReportData(Vector3 position, int networkViewID)
        {
            this.position = position;
            this.networkViewID = networkViewID;
        }
    }
}