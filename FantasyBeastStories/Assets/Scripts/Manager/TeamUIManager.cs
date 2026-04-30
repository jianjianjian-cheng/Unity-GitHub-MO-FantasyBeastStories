using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    public class TeamUIManager : MonoBehaviour
    {
        #region 单例模式
        public static TeamUIManager instance;

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
            }
        }
        #endregion
        private GameObject playerLocal;
        private Text namePlayerLocal;
        private GameObject player1;
        private Text namePlayer1;
        private GameObject player2;
        private Text namePlayer2;
        private GameObject player3;
        private Text namePlayer3;

        void Start()
        {
            Intilize();
        }

        void Intilize()
        {
            playerLocal = GameObject.Find("PlayerLocal");
            player1 = GameObject.Find("Player1");
            player2 = GameObject.Find("Player2");
            player3 = GameObject.Find("Player3");
            if (playerLocal == null)
            {
                Debug.LogError("PlayerLocal 未找到");
                return;
            }
            if (player1 == null)
            {
                Debug.LogError("Player1 未找到");
                return;
            }
            if (player2 == null)
            {
                Debug.LogError("Player2 未找到");
                return;
            }
            if (player3 == null)
            {
                Debug.LogError("Player3 未找到");
                return;
            }
            namePlayerLocal = playerLocal.GetComponentInChildren<Text>();
            namePlayer1 = player1.GetComponentInChildren<Text>();
            namePlayer2 = player2.GetComponentInChildren<Text>();
            namePlayer3 = player3.GetComponentInChildren<Text>();

            SetUpTeamUI();
        }

        private void SetUpTeamUI()
        {
            // 设置本地玩家的UI
            PlayerData localPlayerData = PlayerManager.instance.GetLocalPlayer();
            //只获取前六位字符
            string playerName = localPlayerData.PlayerName;
            int maxLength = Mathf.Min(playerName.Length, 6);
            namePlayerLocal.text = playerName.Substring(0, maxLength);
            // 设置其他玩家的UI
            SetOtherTeamUI();
        }

        public void SetOtherTeamUI()
        {
            #region  设置其他玩家的UI可见性
            switch (PlayerManager.instance.PlayerCount)
            {
                case 1:
                    player1.SetActive(false);
                    player2.SetActive(false);
                    player3.SetActive(false);
                    break;
                case 2:
                    player1.SetActive(true);
                    player2.SetActive(false);
                    player3.SetActive(false);
                    break;
                case 3:
                    player1.SetActive(true);
                    player2.SetActive(true);
                    player3.SetActive(false);
                    break;
                case 4:
                    player1.SetActive(true);
                    player2.SetActive(true);
                    player3.SetActive(true);
                    break;
            }
            #endregion
            List<PlayerData> allPlayers = PlayerManager.instance.PlayerList;
            // 过滤并设置UI
            var otherPlayers = allPlayers
                .Where(p => p.PlayerId != PhotonNetwork.LocalPlayer.UserId)
                .Take(3)
                .ToList();

            Text[] nameTexts = { namePlayer1, namePlayer2, namePlayer3 };

            for (int i = 0; i < nameTexts.Length; i++)
            {
                if (i < otherPlayers.Count && !string.IsNullOrEmpty(otherPlayers[i].PlayerName))
                {
                    string playerName = otherPlayers[i].PlayerName;
                    int maxLength = Mathf.Min(playerName.Length, 6);
                    nameTexts[i].text = playerName.Substring(0, maxLength);
                }
                else
                {
                    nameTexts[i].text = i < otherPlayers.Count ? "未命名" : "";
                }
            }
        }
    }
}
