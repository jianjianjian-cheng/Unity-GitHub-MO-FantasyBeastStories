using System.Collections;
using System.Collections.Generic;
using Controllers.Enemy;
using UnityEngine;

namespace Controllers.Combat
{
  public class TriggerBase : MonoBehaviour
  {
    // Start is called before the first frame update
    public virtual void Start()
    {

    }

    public virtual void Update()
    {

    }

    public virtual void OnTriggerEnter(Collider other)
    {
      var enemyBase = other.gameObject.GetComponent<EnemyBase>();
      if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
      {
        return;
      }

    }

    public virtual void OnTriggerStay(Collider other)
    {
      var enemyBase = other.gameObject.GetComponent<EnemyBase>();
      if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
      {
        return;
      }

    }

    public virtual void OnTriggerExit(Collider other)
    {
      var enemyBase = other.gameObject.GetComponent<EnemyBase>();
      if (!other.gameObject.CompareTag("Enemy") || enemyBase == null || enemyBase.GetIsDie())
      {
        return;
      }
    }
  }
}