using Core.Channels.Base;
using UnityEngine;

namespace Core.Channels.Game
{
  public enum DifficultyQueryType
  {
    GetDifficultyCoefficient,
    GetPlayerCount
  }

  public class DifficultyCoefficientQueryData : EventArgsBase
  {
    public DifficultyQueryType queryType;
    public float result;
    public int playerCount;
  }

  [CreateAssetMenu(menuName = "Events/Game/Difficulty Coefficient Query Event Channel")]
  public class DifficultyCoefficientQueryEventChannelSO : BaseEventChannelSO<DifficultyCoefficientQueryData>
  {
  }
}
