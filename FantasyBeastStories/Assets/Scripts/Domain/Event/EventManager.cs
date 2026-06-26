using System.Collections.Generic;
using Domain.Character.Attribute;
using Domain.Event.Channels.Player;
using Photon.Pun;
using UnityEngine;

namespace Domain.Event
{
  /// <summary>
  /// 玩家属性缓存管理器
  /// 监听 PlayerAttributeEventChannelSO 维护 (ActorNumber, AttributeKey) → AttributePlayerBase 的字典
  /// 供 AttackRangeBase / PlayerController 等通过 SO 通道查询属性
  /// 清理后：仅保留属性缓存功能，移除所有旧版字符串事件字典（已由 SO 事件通道替代）
  /// </summary>
  public class EventManager : MonoBehaviour
  {
    #region 单例模式
    public static EventManager instance;

    void Awake()
    {
      if (instance == null)
      {
        instance = this;
        DontDestroyOnLoad(gameObject);
      }
      else
      {
        Destroy(gameObject);
        return;
      }

      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.playerAttributeChannel.RegisterListener(OnPlayerAttributeQuery);
    }

    void OnDestroy()
    {
      if (EventChannelLocator.MainContainer != null)
        EventChannelLocator.MainContainer.playerAttributeChannel.UnregisterListener(OnPlayerAttributeQuery);
    }
    #endregion

    /// <summary>
    /// 属性缓存字典：(actorNumber, attributeKey) → AttributePlayerBase
    /// </summary>
    private Dictionary<(int actorNumber, string key), AttributePlayerBase> attributePlayerBaseDictionary =
        new Dictionary<(int, string), AttributePlayerBase>();

    /// <summary>
    /// 接收 PlayerAttributeEventChannelSO 的回调
    /// 处理 RegisterAttribute / UnregisterAttribute / GetAttributeById / GetLocalPlayerAttribute
    /// </summary>
    private void OnPlayerAttributeQuery(PlayerAttributeData data)
    {
      switch (data.queryType)
      {
        case PlayerAttributeQueryType.RegisterAttribute:
          if (int.TryParse(data.playerId, out int actorNum))
          {
            var key = (actorNum, data.attributeName);
            attributePlayerBaseDictionary[key] = data.attribute;
          }
          break;
        case PlayerAttributeQueryType.UnregisterAttribute:
          if (int.TryParse(data.playerId, out int unregActorNum))
          {
            var unregKey = (unregActorNum, data.attributeName);
            attributePlayerBaseDictionary.Remove(unregKey);
          }
          break;
        case PlayerAttributeQueryType.GetAttributeById:
          if (int.TryParse(data.playerId, out int getActorNum))
          {
            var getKey = (getActorNum, data.attributeName);
            attributePlayerBaseDictionary.TryGetValue(getKey, out data.attribute);
          }
          break;
        case PlayerAttributeQueryType.GetLocalPlayerAttribute:
          int localActorNum = -1;
          try { localActorNum = PhotonNetwork.LocalPlayer.ActorNumber; }
          catch { }
          var localKey = (localActorNum, data.attributeName);
          attributePlayerBaseDictionary.TryGetValue(localKey, out data.attribute);
          break;
      }
    }

    /// <summary>
    /// 外部注册属性（旧接口兼容，保留对外提供）
    /// </summary>
    public void RegisterAttributePlayerBase(int actorNumber, string key, AttributePlayerBase attributePlayerBase)
    {
      var dictKey = (actorNumber, key);
      if (attributePlayerBaseDictionary.ContainsKey(dictKey))
        attributePlayerBaseDictionary[dictKey] = attributePlayerBase;
      else
        attributePlayerBaseDictionary.Add(dictKey, attributePlayerBase);
    }

    /// <summary>
    /// 外部注销属性
    /// </summary>
    public void UnRegisterAttributePlayerBase(int actorNumber, string key)
    {
      var dictKey = (actorNumber, key);
      if (attributePlayerBaseDictionary.ContainsKey(dictKey))
        attributePlayerBaseDictionary.Remove(dictKey);
    }

    /// <summary>
    /// 外部查询属性
    /// </summary>
    public AttributePlayerBase GetAttributePlayerBase(int actorNumber, string key)
    {
      var dictKey = (actorNumber, key);
      if (attributePlayerBaseDictionary.TryGetValue(dictKey, out var value))
        return value;
      return null;
    }

    /// <summary>
    /// 获取本地玩家属性（便捷方法）
    /// </summary>
    public AttributePlayerBase GetLocalPlayerAttribute(string key)
    {
      if (PhotonNetwork.LocalPlayer != null)
        return GetAttributePlayerBase(PhotonNetwork.LocalPlayer.ActorNumber, key);
      return null;
    }
  }
}