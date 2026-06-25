using System.Collections;
using Infrastructure.Network;
using Domain.Manager;
using Photon.Pun;
using UnityEngine;
using Domain.Event;

namespace Domain.Item
{
    public class DropItemBase : MonoBehaviourPun
    {
        [Header("掉落参数")]
        [SerializeField]
        private float explosionForce = 3f;

        [SerializeField]
        private float upwardForce = 4f;

        [SerializeField]
        private float lifeTime = 2f;

        [SerializeField]
        private float flyToPlayerSpeed = 5f;

        [Header("拾取弹开参数")]
        [SerializeField]
        private float pushBackForce = 3f; // 向后弹开的力度

        [SerializeField]
        private float pushBackDelay = 0.2f; // 弹开后延迟多久开始飞向玩家

        private Rigidbody rb;
        private GameObject moveTarget;
        private bool isFlyingToPlayer = false;
        private Coroutine flyCoroutine;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        protected virtual void Start() { }

        protected virtual void Update()
        {
            if (!isFlyingToPlayer || moveTarget == null)
                return;

            // 飞向玩家
            Vector3 direction = (moveTarget.transform.position - transform.position).normalized;
            transform.position += direction * flyToPlayerSpeed * UnityEngine.Time.deltaTime;

            // 到达后回收
            if (Vector3.Distance(transform.position, moveTarget.transform.position) < 0.3f)
            {
                OnReachPlayer();
            }
        }

        protected virtual void OnEnable()
        {
            isFlyingToPlayer = false;
            moveTarget = null;
            ApplyExplosionEffect();
        }

        protected virtual void OnDisable()
        {
            isFlyingToPlayer = false;
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

            Vector3 force = (randomDirection * explosionForce) + (Vector3.up * upwardForce);
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
            if (isFlyingToPlayer)
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
                rb.AddForce(pushBackDirection * pushBackForce + Vector3.up * 2f, ForceMode.Impulse);
            }

            // 延迟后开始飞向玩家
            flyCoroutine = StartCoroutine(DelayedFlyToPlayer());
        }

        private IEnumerator DelayedFlyToPlayer()
        {
            // 等待弹开动画
            yield return new WaitForSeconds(pushBackDelay);

            // 关闭物理，准备飞行
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            isFlyingToPlayer = true;
        }

        public virtual void ResetState() { }

        protected virtual void OnReachPlayer()
        {
            if (EventChannelLocator.MainContainer.gameSettings.IsTest)
            {
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateDespawn(NetworkObjectPoolConst.ExperienceBall_Blue, gameObject));
                return;
            }
            if (PhotonNetwork.IsMasterClient)
            {
                EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                    PoolOperationData.CreateDespawn(NetworkObjectPoolConst.ExperienceBall_Blue, gameObject));
                return;
            }
            photonView.RPC("RPC_DespawnItem", RpcTarget.MasterClient);
        }

        [PunRPC]
        public void RPC_DespawnItem()
        {
            EventChannelLocator.MainContainer.poolOperationChannel.Raise(
                PoolOperationData.CreateDespawn(NetworkObjectPoolConst.ExperienceBall_Blue, gameObject));
        }
    }
}