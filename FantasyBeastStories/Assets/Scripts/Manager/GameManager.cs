using System.Collections;
using System.Collections.Generic;
using Other;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Manager
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> spawnPoints = new List<GameObject>(); // 生成点列表
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

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Intilize()
        {
            FindSpawnPoints();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // FindSpawnPoints();
        }

        public void FindSpawnPoints()
        {
            GameObject[] spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            spawnPointsList = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPointsList.Length == 0)
            {
                Debug.LogError("没有找到生成点");
                return;
            }
            foreach (GameObject spawnPoint in spawnPointsList)
            {
                Debug.Log("找到生成点: " + spawnPoint.name);
                spawnPoints.Add(spawnPoint);
            }
        }

        public GameObject GetEmptySpawnPoint()
        {
            foreach (GameObject spawnPoint in spawnPoints)
            {
                if (spawnPoint.GetComponent<SpawnPoint>().isEmpty)
                {
                    Debug.Log("返回空闲的生成点: " + spawnPoint.name);
                    return spawnPoint;
                }
            }
            Debug.LogWarning("没有空闲的生成点了");
            return null;
        }
    }
}
