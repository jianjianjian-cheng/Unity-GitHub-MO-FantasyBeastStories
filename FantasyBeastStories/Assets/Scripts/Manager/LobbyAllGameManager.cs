using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Manager
{
    public class LobbyAllGameManager : MonoBehaviour
    {
        [SerializeField] private PlayableDirector vcTimeLine;
        private Button startButton;
        private Button exitButton;
        private Button optionButton;
        private GameObject gameNameModel;

        private void Start()
        {
            vcTimeLine = GameObject.Find("Director").GetComponent<PlayableDirector>();
            startButton = GameObject.Find("StartButton").GetComponent<Button>();
            exitButton = GameObject.Find("ExitButton").GetComponent<Button>();
            optionButton = GameObject.Find("OptionsButton").GetComponent<Button>();
            startButton.onClick.AddListener(Startbutton);
            exitButton.onClick.AddListener(Exitbutton);
            optionButton.onClick.AddListener(Optionsbutton);
            gameNameModel = GameObject.Find("GameNameModel");
        }
        public void Startbutton()
        {
            vcTimeLine.Play();
        }

        public void Exitbutton()
        {
            Application.Quit();
        }

        public void Optionsbutton()
        {
            Debug.Log("Optionsbutton");
        }
    }
}
