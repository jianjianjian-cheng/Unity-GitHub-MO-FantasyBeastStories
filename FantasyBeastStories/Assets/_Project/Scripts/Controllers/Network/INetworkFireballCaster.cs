using Core;
using UnityEngine;

namespace Controllers.Network
{
    public interface INetworkFireballCaster
    {
        void RequestFireball(Vector3 spawnPos, Vector3 direction, float speed, Element element);

        /// <summary>
        /// 请求广播 GuiLing（鬼灵弹）发射 — 向其他客户端同步视觉投射物
        /// </summary>
        /// <param name="spawnPos">生成位置</param>
        /// <param name="direction">发射方向（已包含随机扩散偏移）</param>
        /// <param name="targetViewID">目标的 PhotonView.ViewID</param>
        /// <param name="elementInt">元素类型 int 值</param>
        void RequestGuiLingCast(Vector3 spawnPos, Vector3 direction, int targetViewID, int elementInt);
    }
}