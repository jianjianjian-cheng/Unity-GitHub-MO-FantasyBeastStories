using System.Collections;
using Domain.Event;
using Domain.Network;
using Domain.Pool;
using Domain.Services;
using UnityEngine;

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

    // ── 受保护的访问器，供子类使用 ──
    protected Rigidbody Rb => rb;
    protected DropItemData DropItemData => dropItemData;
    protected NetworkIdentityBase Network => _network;
    protected GameObject MoveTarget => moveTarget;
    protected Coroutine FlyCoroutine { get => flyCoroutine; set => flyCoroutine = value; }

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

    protected virtual void ApplyExplosionEffect()
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
      // 通过 IDomainRpcService 发送 RPC 到 MasterClient，附带 ViewID
      if (_network != null)
      {
        NetworkServiceLocator.DomainRpcService?.InvokeRPC(
            "RPC_DespawnItem",
            Domain.Services.NetworkTarget.MasterClient,
            _network.ViewID);
      }
    }

    /// <summary>
    /// 由 DomainRpcBridge.RPC_DespawnItem 调用 — 在 MasterClient 上执行销毁
    /// </summary>
    public virtual void HandleDespawnItem()
    {
      EventChannelLocator.MainContainer.poolOperationChannel.Raise(
          PoolOperationData.CreateDespawn(PoolConst.ExperienceBall_Blue, gameObject));
    }
  }
}