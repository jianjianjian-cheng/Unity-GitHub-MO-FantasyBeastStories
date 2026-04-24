using System.Collections;
using System.Collections.Generic;
using Events;
using FX;
using Manager;
using Photon.Pun;
using Trigger;
using UnityEngine;

namespace Photon.CastSciprt
{
    public class CastNetwork : MonoBehaviourPun
    {
        /// <summary>
        /// 请求发射火球（由 AttackRangeBase 调用）
        /// 功能：向其他玩家广播发射指令
        /// </summary>
        public void RequestFireball(Vector3 spawnPos, Vector3 direction, float speed)
        {
            // 只发给其他玩家（RpcTarget.Others），本地玩家已经在 AttackRangeBase 中生成了
            photonView.RPC("RPC_OnFireballCast", RpcTarget.Others, spawnPos, direction, speed);
        }


        /// <summary>
        /// RPC：其他玩家收到后，在本地生成火发射物
        /// </summary>
        [PunRPC]
        void RPC_OnFireballCast(Vector3 spawnPos, Vector3 direction, float speed)
        {
            // 获取炮塔的 AttackRangeBase 组件来生成本地发射物
            AttackRangeBase attackRange = GetComponent<AttackRangeBase>();
            if (attackRange != null)
            {
                SpawnFireballForOthers(spawnPos, direction);
            }
        }

        /// <summary>
        /// 为其他玩家生成本地火球（isMine = false，不负责伤害判定）
        /// </summary>
        private void SpawnFireballForOthers(Vector3 spawnPos, Vector3 direction)
        {
            // 1. 生成视觉特效
            GameObject visualObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                ObjectPoolConst.ImpactCannonCommonPool, spawnPos);
            if (visualObj != null)
            {
                visualObj.GetComponentInChildren<ParticleSystem>()?.Play();
                visualObj.transform.rotation = Quaternion.LookRotation(direction);
            }

            // 2. 生成碰撞触发器（关键：isMine = false）
            GameObject triggerObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                ObjectPoolConst.ImpactCannonTriggerPool, spawnPos);
            if (triggerObj != null)
            {
                ImpactCannon cannon = triggerObj.GetComponent<ImpactCannon>();
                if (cannon == null)
                {
                    cannon = triggerObj.AddComponent<ImpactCannon>();
                }
                // 重要：传入 isMine = false，这个火球不会判定伤害
                cannon.StartShoot(direction, isMine: false);
            }
        }

        // ==================== 伤害相关 ====================

        /// <summary>
        /// 广播伤害给所有客户端（由 ImpactCannon 调用）
        /// </summary>
        public void BroadcastDamage(GameObject enemyObj, float damage, bool isCritical,
            float criticalMultiplier, Vector3 hitPoint)
        {
            PhotonView enemyView = enemyObj.GetComponent<PhotonView>();
            if (enemyView == null) return;

            // 发给其他玩家（RpcTarget.Others）
            photonView.RPC("RPC_DealDamage", RpcTarget.Others,
                enemyView.ViewID,
                damage,
                isCritical,
                criticalMultiplier,
                hitPoint);
            photonView.RPC("RPC_ShowDamageNum", RpcTarget.All,
                (int)damage,
                hitPoint,
                isCritical);
        }

        /// <summary>
        /// RPC：所有客户端执行扣血和特效
        /// </summary>
        [PunRPC]
        void RPC_DealDamage(int enemyViewID, float damage, bool isCritical,
            float criticalMultiplier, Vector3 hitPoint)
        {
            // 1. 通过 ViewID 找到敌人对象
            PhotonView enemyView = PhotonView.Find(enemyViewID);
            if (enemyView == null) return;

            // 2. 播放命中特效（所有客户端都看到）
            string poolKey = ObjectPoolConst.ImpactCannonHitCommonPool;
            GameObject hitEffect = ObjectPoolManager.instance.GetFromPoolAndActivate(poolKey, hitPoint);
            if (hitEffect != null)
            {
                hitEffect.GetComponentInChildren<ParticleSystem>()?.Play();
            }

            // 3. 触发伤害事件（扣血）
            DamageEventArgs damageEventArgs = new DamageEventArgs(
                DamageType.Fire,
                gameObject,
                enemyView.gameObject,
                damage,
                isCritical,
                criticalMultiplier
            );

            EventManager.instance.TriggerEventComplex(EventNames.DamageReceived, damageEventArgs);
        }

        /// <summary>
        /// RPC：所有客户端显示伤害数字
        /// 用于在敌人被攻击时，显示伤害数字    
        /// </summary>
        /// <param name="damageValue"></param>
        /// <param name="position"></param>
        [PunRPC]
        void RPC_ShowDamageNum(int damageValue, Vector3 position, bool isCritical)
        {
            Vector3 spawnPos = position + Vector3.up * 0f;
            // 1. 从对象池获取伤害数字对象
            GameObject damageNumObj = ObjectPoolManager.instance.GetFromPoolAndActivate(
                ObjectPoolConst.DamageNumPool, spawnPos);
            if (damageNumObj != null)
            {
                DamageNum damageNum = damageNumObj.GetComponent<DamageNum>();
                if (damageNum != null)
                {
                    damageNum.Play(damageValue, spawnPos, isCritical);
                }
            }
            else
            {
                Debug.LogError($"DamageNumPool 为空，无法显示伤害数字：{damageValue}, {position}, {isCritical}");
            }
        }
    }
}
