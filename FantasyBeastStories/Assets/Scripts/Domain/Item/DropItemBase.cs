using System.Collections;
using Domain.Pool;
using Domain.Services;
using Application;
using UnityEngine;

namespace Domain.Item
{
  public class DropItemBase : MonoBehaviour
  {
    [Header("纯数据")]
    [SerializeField]
    private DropItemData dropItemData;

    private Rigidbody rb;
    private GameObject moveTarget;
    private Coroutine flyCoroutine;

    // ── 受保护的访问器，供子类使用 ──
    protected Rigidbody Rb => rb;
    protected DropItemData DropItemData => dropItemData;
    protected GameObject MoveTarget => moveTarget;
    protected Coroutine FlyCoroutine { get => flyCoroutine; set => flyCoroutine = value; }

    protected virtual void Awake()
    {
      dropItemData = new DropItemData();
      rb = GetComponent<Rigidbody>();
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

    /// <summary>
    /// 球到达玩家时调用（由子类 override 实现具体拾取逻辑）
    /// </summary>
    protected virtual void OnReachPlayer()
    {
      // 默认行为：子类应重写此方法实现拾取逻辑
      // 经验球子类已重写为：触发经验事件 → 上报房主 → 回本地池
    }
  }
}