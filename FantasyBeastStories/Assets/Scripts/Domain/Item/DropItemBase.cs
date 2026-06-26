using System.Collections;
using Domain.Event;
using Domain.Network;
using Domain.Pool;
using UnityEngine;
using Photon.Pun;

namespace Domain.Item
{
  public class DropItemBase : MonoBehaviour
  {
    [SerializeField] private NetworkIdentityBase _network;

    [Header("纯数据")]
    [SerializeField]
    private DropItemData dropItemData;

    private Rigidbody rb;
    private GameObject moveTarget;
    private Coroutine flyCoroutine;

    protected virtual void Awake()
    {
      dropItemData = new DropItemData();
      rb = GetComponent<Rigidbody>();
      if (_network == null)
        _network = GetComponent<NetworkIdentityBase>();
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
      if (!dropItemData.isFlyingToPlayer || moveTarget == null)
        return;

      // 飞向玩家
      Vector3 direction = (moveTarget.transform.position - transform.position).normalized;
      transform.position += direction * dropItemData.flyToPlayerSpeed * UnityEngine.Time.deltaTime;

      // 到达后回收
      if (Vector3.Distance(transform.position, moveTarget.transform.position) < 0.3f)
      {
        OnReachPlayer();
      }
    }

    protected virtual void OnEnable()
    {
      dropItemData.isFlyingToPlayer = false;
      moveTarget = null;
      ApplyExplosionEffect();
    }

    protected virtual void OnDisable()
    {
      dropItemData.isFlyingToPlayer = false;
      moveTarget = null;
      if (flyCoroutine != null)
      {
        StopCoroutine(flyCoroutine);
        flyCoroutine = null;
      }
    }

    private void ApplyExplosionEffect()
    {
      if (rb == null)
        return;

      // Debug.LogWarning("ApplyExplosionEffect");
      rb.velocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;

      Vector2 randomCircle = Random.insideUnitCircle;
      Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y).normalized;

      Vector3 force = (randomDirection * dropItemData.explosionForce) + (Vector3.up * dropItemData.upwardForce);
      rb.AddForce(force, ForceMode.Impulse);
    }

    public void ExplodeAtPosition(Vector3 centerPosition, float radius = 1f)
    {
      transform.position = centerPosition + Random.insideUnitSphere * radius;
      transform.position = new Vector3(
          transform.position.x,
          centerPosition.y,
          transform.position.z
      );
      ApplyExplosionEffect();
    }

    public virtual void HandlePickupEnter(GameObject player)
    {
      if (dropItemData.isFlyingToPlayer)
        return;

      moveTarget = player;

      // 先向后弹开
      Vector3 pushBackDirection = (transform.position - player.transform.position).normalized;
      pushBackDirection.y = 0; // 保持水平方向

      if (rb != null)
      {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = false; // 暂时开启物理，让弹开力生效
        rb.AddForce(pushBackDirection * dropItemData.pushBackForce + Vector3.up * 2f, ForceMode.Impulse);
      }

      // 延迟后开始飞向玩家
      flyCoroutine = StartCoroutine(DelayedFlyToPlayer());
    }

    private IEnumerator DelayedFlyToPlayer()
    {
      // 等待弹开动画
      yield return new WaitForSeconds(dropItemData.pushBackDelay);

      // 关闭物理，准备飞行
      if (rb != null)
      {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
      }

      dropItemData.isFlyingToPlayer = true;
    }

    public virtual void ResetState() { }

    protected virtual void OnReachPlayer()
    {
      if (EventChannelLocator.MainContainer.gameSettings.IsTest)
      {
        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            PoolOperationData.CreateDespawn(PoolConst.ExperienceBall_Blue, gameObject));
        return;
      }
      if (_network.IsMasterClient)
      {
        EventChannelLocator.MainContainer.poolOperationChannel.Raise(
            PoolOperationData.CreateDespawn(PoolConst.ExperienceBall_Blue, gameObject));
        return;
      }
      _network.RPC("RPC_DespawnItem", NetworkTarget.MasterClient);
    }

    [PunRPC]
    public void RPC_DespawnItem()
    {
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateDespawn(PoolConst.ExperienceBall_Blue, gameObject));
    }
  }
}