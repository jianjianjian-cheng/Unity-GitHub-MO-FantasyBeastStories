using UnityEngine;

namespace Domain.Event
{
    /// <summary>
    /// 道具拾取事件数据
    /// 用于UI显示、成就统计等
    /// </summary>
    public class PowerUpCollectEventData
    {
        public string itemName;
        public string effectName;
        public Vector3 collectPosition;
        public int playerId;
    }
}