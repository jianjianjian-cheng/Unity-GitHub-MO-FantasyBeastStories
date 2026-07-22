using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Combat
{
    [CreateAssetMenu(menuName = "Events/Combat/Enemy Report Event Channel")]
    public class EnemyReportEventChannelSO : BaseEventChannelSO<EnemyReportData>
    {
    }

    public enum EnemyReportType
    {
        Kill,
        EscortArrive,
    }

    public class EnemyReportData : EventArgsBase
    {
        public Vector3 position;
        public int networkViewID;
        public EnemyReportType reportType;

        public EnemyReportData(Vector3 position, int networkViewID, EnemyReportType reportType = EnemyReportType.Kill)
        {
            this.position = position;
            this.networkViewID = networkViewID;
            this.reportType = reportType;
        }
    }
}