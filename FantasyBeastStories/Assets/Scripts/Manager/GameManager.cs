using System.Collections;
using System.Collections.Generic;
using Other;
using UnityEngine;
using UnityEngine.UIElements;

namespace Manager
{
    public class GameManager : MonoBehaviour
    {
        private List<Transform> spawnPoints = new List<Transform>(); // 生成点列表
        //静态全局变量isTest，控制是否进入测试模式
        public static bool isTest; // 是否测试模式
        public static bool isStayLobby = true; // 是否在大厅lobby场景
        [SerializeField] private bool isTestInspector; // 在Inspector面板中设置的测试模式
        public static GameManager instance;
        void Awake()
        {
            isTest = isTestInspector;
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            Intilize();
        }

        private void Intilize()
        {
            FindSpawnPoints();
        }

        public void FindSpawnPoints()
        {
            GameObject[] spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPoints.Clear();
            spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (GameObject spawnPoint in spawnPointsList)
            {
                spawnPoints.Add(spawnPoint.transform);
            }
        }

        public Transform GetEmptySpawnPoint()
        {
            foreach (Transform spawnPoint in spawnPoints)
            {
                if (spawnPoint.GetComponent<SpawnPoint>().isEmpty)
                {
                    return spawnPoint;
                }
            }
            Debug.LogWarning("没有空闲的生成点了");
            return null;
        }
    }
}
